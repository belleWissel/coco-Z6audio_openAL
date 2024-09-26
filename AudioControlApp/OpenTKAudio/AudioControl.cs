using System;
using System.Configuration;
using OpenTKAudio.Engine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.IO;
using System.Threading;
using AudioControlApp.Utils;
using SecondstoryCommon; // used for AppEventHandler

//using NVorbis.OpenTKSupport;

namespace OpenTKAudio.Engine
{
    public class AudioControl
    {
        //private int myID = -1;
        private AudioContext myAudioContext;
        private static int maxNumberOfAudioDev = 20; // Dante system introduces at least 16 devices... ?
        private int actualNumberOfAudioDev = 0;
        private string[] audioDevList = new string[maxNumberOfAudioDev];

        private string audioDevNameEmployed = "speakers";
        private int audioDevIDEmployed = 0;
        //private bool isPlayingSound = false;
        //private bool doPlay = false;

        private static int maxNumberOfVoices = 10; // check that this matches readXML limits
        private int[] actualNumberOfAmbientVoices = new int[2];
        private SingleVoiceControl[,] ambientVoiceControl = new SingleVoiceControl[maxNumberOfVoices,2];

        private static int maxNumberOfBuildVoices = 5; // check that this matches readXML limits
        private int actualNumberOfBuildVoices = 0;
        private SingleVoiceControl[] buildVoiceControl = new SingleVoiceControl[maxNumberOfBuildVoices];

        private static int maxNumberOfHighlightVoices = 10; // check that this matches readXML limits
        private int actualNumberOfHighlightVoices = 0;
        private SingleVoiceControl[] highlightVoiceControl = new SingleVoiceControl[maxNumberOfHighlightVoices];

        
        //private string myAppName = "audioControl0";
        private int myAppID = 0;
        private LoadAudioSettingsFromXML openAndReadXML;
        
        // ********************************************************
        // ambient playback sequencer
        private int[] currentlyActiveAmbientFile = new int[2]; // one for each channel
        private bool ambientModeIsPlaying = false;

        // ********************************************************
        // build playback sequencer
        private int currentlyActiveBuildFile = 0; // one for each channel
        private bool buildLoopIsPlaying = false;
        
        // ********************************************************
        // highlight playback sequencer
        private int currentlyActiveHighlightFile = 0; // one for each channel
        private bool highlightLoopIsPlaying = false;
        
        // ********************************************************
        // stateMachine
        private string currentState = "noPresenence"; // "build" or "highlight"
        private string prevState = "noPresenence"; // "build" or "highlight"
       
        

        public AudioControl(int whichkioskID, string whichSharedAssetPath)
        {
            myAppID = whichkioskID;
            
            openAndReadXML = new LoadAudioSettingsFromXML(whichSharedAssetPath);
            int numberOfAmbientFilesToLoad;

            int i, channel;

            string whichPath;
            string whichFileName;
            string whichFileAndPath;
            int whichChannel;
            float whichLength;

            whichPath = openAndReadXML.ambientLoopFilePath;
            
            
            // create all voiceControl instances here:
            
            // ambient first:
            
            for (channel = 0; channel < 2; ++channel) // channels 0 and 1
            {
                actualNumberOfAmbientVoices[channel] = openAndReadXML.actualNumberOfAmbientLoopFilesPerChannel[myAppID, channel];
                for (i = 0; i < actualNumberOfAmbientVoices[channel]; ++i)
                {
                    if (myAppID == 0) // had to separate arrays for name, length
                    {
                        whichFileName = openAndReadXML.ambientFileName0[i, channel];
                        whichLength = openAndReadXML.ambientFileLength0[i, channel];
                    }
                    else if (myAppID == 1)
                    {
                        whichFileName = openAndReadXML.ambientFileName1[i, channel];
                        whichLength = openAndReadXML.ambientFileLength1[i, channel];
                    }
                    else
                    {
                        whichFileName = openAndReadXML.ambientFileName2[i, channel];
                        whichLength = openAndReadXML.ambientFileLength2[i, channel];
                    }

                    whichFileAndPath = whichPath + whichFileName;
                    ambientVoiceControl[i, channel] = new SingleVoiceControl(i, channel, true, false, whichFileAndPath, whichLength);
                    ambientVoiceControl[i, channel].assignAmbientOverlapTime(openAndReadXML.ambientOverlap);
                    ambientVoiceControl[i, channel].assignMaxVolume(openAndReadXML.ambientVol);
                    ambientVoiceControl[i, channel].OnEvent += new AppEventHandler(ambientVoice_OnEvent);
                }
            }

            // build files second:
            whichPath = openAndReadXML.buildFilePath;
            
            actualNumberOfBuildVoices = openAndReadXML.actualNumberOfBuildFiles[myAppID];
            for (i = 0; i < actualNumberOfBuildVoices; ++i)
            {
                whichFileName = openAndReadXML.buildFileName[i];
                whichLength = openAndReadXML.buildFileLength[i];
                whichFileAndPath = whichPath + whichFileName;
                buildVoiceControl[i] = new SingleVoiceControl(i, -1, false,  true, whichFileAndPath, whichLength);
                buildVoiceControl[i].assignBuildOverlapTime(openAndReadXML.buildOverlapTime);
                buildVoiceControl[i].assignBuildRampTime(openAndReadXML.buildRampUpTime, openAndReadXML.buildRampDownTime);
                buildVoiceControl[i].assignMaxVolume(openAndReadXML.buildVol);

                buildVoiceControl[i].OnEvent += new AppEventHandler(buildVoice_OnEvent);

            }

            // highlight files last:
            whichPath = openAndReadXML.highlightFilePath;
            
            actualNumberOfHighlightVoices = openAndReadXML.actualNumberOfHighlightFiles[myAppID];
            for (i = 0; i < actualNumberOfHighlightVoices; ++i)
            {
                whichFileName = openAndReadXML.highlightFileName[i];
                whichLength = openAndReadXML.highlightFileLength[i];
                whichFileAndPath = whichPath + whichFileName;
                highlightVoiceControl[i] = new SingleVoiceControl(i, -1, false, false, whichFileAndPath, whichLength);
                highlightVoiceControl[i].assignHighlightOverlapTime(openAndReadXML.highlightOverlapTime);
                highlightVoiceControl[i].assignHighlightRampTime(openAndReadXML.highlightRampUpTime, openAndReadXML.highlightRampDownTime);
                highlightVoiceControl[i].assignMaxVolume(openAndReadXML.highlightVol);
                highlightVoiceControl[i].OnEvent += new AppEventHandler(highlightVoice_OnEvent);

            }
        }

        public void initialize()
        {
            listAvailableAudioDevices();
            assignAudioDevToAllTracks();

        }

        
        public void update() // draw loop
        {
            int i, channel;
            
            for (channel = 0; channel < 2; ++channel) // channels 0 and 1
            {
                for (i = 0; i < actualNumberOfAmbientVoices[channel]; ++i)
                {
                    ambientVoiceControl[i, channel].update();
                }
            }
            
            for (i = 0; i < actualNumberOfBuildVoices; ++i)
            {
                buildVoiceControl[i].update();
            }
            for (i = 0; i < actualNumberOfHighlightVoices; ++i)
            {
                highlightVoiceControl[i].update();
            }

            if (currentState != prevState) // something changed since last draw
            {
                if (currentState == "highlight")
                {
                    rampUpHighlights();
                    rampDownBuilds();
                    startHighlightLoop();
                }
                else if ( currentState == "build")
                {
                    rampDownHighlights();
                    rampUpBuilds();
                    startBuildLoop();
                }
                else
                {
                    rampDownHighlights();
                    rampDownBuilds();
                }
                
                prevState = currentState;
            }

        }
        //public bool someoneIsInTheMiddle = false;
        //public bool someoneIsInTheEdge = false;

        public void setStateTo(string whichState)
        {
            currentState = whichState;
        }
        
        /*
        public void centerPresenceDetected()
        {
            if (!someoneIsInTheMiddle)
            {
                someoneIsInTheMiddle = true;
                startHighlightLoop();
                edgePresenceNotDetected(); // disable build (if active)
            }
        }

        public void centerPresenceNotDetected()
        {
            if (someoneIsInTheMiddle)
            {
                someoneIsInTheMiddle = false;
                stopHighlightLoops();
            }
        }

        public void edgePresenceDetected()
        {
            if (!someoneIsInTheMiddle) // don't play build if middle is active
            {
                if (!someoneIsInTheEdge) // we're not already playing/started
                {
                    someoneIsInTheEdge = true;
                    startBuildLoop();
                }
            }
        }

        public void edgePresenceNotDetected()
        {
            if (someoneIsInTheEdge) // don't play build if middle is active
            {
                someoneIsInTheEdge = false;
                stopBuildLoops();
            }
        }*/
        
        
        private void assignAudioDevToAllTracks()
        {
            int devToEmploy = openAndReadXML.audioDevID[myAppID];
            
            audioDevNameEmployed = audioDevList[devToEmploy];
            audioDevIDEmployed = devToEmploy;
            
            myAudioContext = new AudioContext(audioDevNameEmployed);
            
            
            int i, channel;

            System.Diagnostics.Debug.WriteLine("[VOXCTRL] loading ambient tracks");

            for (channel = 0; channel < 2; ++channel) // channels 0 and 1
            {
                actualNumberOfAmbientVoices[channel] = openAndReadXML.actualNumberOfAmbientLoopFilesPerChannel[myAppID, channel];
                for (i = 0; i < actualNumberOfAmbientVoices[channel]; ++i)
                {
                    //ambientVoiceControl[i, channel].setAudioDevice(audioDevNameEmployed);
                    ambientVoiceControl[i, channel].setAudioContext(myAudioContext);
                    
                }
            }
            System.Diagnostics.Debug.WriteLine("[VOXCTRL] loading build tracks");

            for (i = 0; i < actualNumberOfBuildVoices; ++i)
            {
                buildVoiceControl[i].setAudioContext(myAudioContext);
            }
            System.Diagnostics.Debug.WriteLine("[VOXCTRL] loading highlight tracks");

            for (i = 0; i < actualNumberOfHighlightVoices; ++i)
            {
                highlightVoiceControl[i].setAudioContext(myAudioContext);
            }
        }

        public void startAmbientLoops()
        {
            if (ambientModeIsPlaying)
                stopAmbientLoops(); // in case any are running
            
            Thread.Sleep (300);
            
            ambientModeIsPlaying = true;
            currentlyActiveAmbientFile[0] = 0;
            currentlyActiveAmbientFile[1] = 0;

            // start both channels from the top:

            if (actualNumberOfAmbientVoices[0] > 0) // only if any ambient voices have been assigned to this channel
                ambientVoiceControl[0, 0].startPlayback();
            if (actualNumberOfAmbientVoices[1] > 0)
                ambientVoiceControl[0, 1].startPlayback();
        }
  
        
        public void startBuildLoop()
        {
            if (actualNumberOfBuildVoices > 0) // only if any build voices have been assigned to this channel
            {
                //if (buildModeIsPlaying)
                //    stopBuildLoops(); // in case any are running

                //Thread.Sleep(300);

                if (!buildLoopIsPlaying)
                {
                    buildLoopIsPlaying = true;
                    int nextFileToPlay = currentlyActiveBuildFile + 1; // start where we left off
                    if (nextFileToPlay >= actualNumberOfBuildVoices)
                        nextFileToPlay = 0;

                    currentlyActiveBuildFile = nextFileToPlay;

                    buildVoiceControl[currentlyActiveBuildFile].startPlayback();
                }

                //for (int i = 0; i < actualNumberOfBuildVoices; ++i)
                //{
                //    buildVoiceControl[i].startMasterFadeIn(); // this begins ramping up all build voice master volume
                //}
            }
        }
        
        public void startHighlightLoop()
        {
            if (actualNumberOfHighlightVoices > 0) // only if any highlight voices have been assigned to this channel
            {

                
                //if (highlightModeIsPlaying)
                //    stopHighlightLoops(); // in case any are running

                //Thread.Sleep(300);

                if (!highlightLoopIsPlaying) // select and start next highlight audio (if not already playing)
                {
                    highlightLoopIsPlaying = true;
                    int nextFileToPlay = currentlyActiveHighlightFile + 1; // start where we left off
                    if (nextFileToPlay >= actualNumberOfHighlightVoices)
                        nextFileToPlay = 0;

                    currentlyActiveHighlightFile = nextFileToPlay;

                    highlightVoiceControl[currentlyActiveHighlightFile].startPlayback();
                }
                
            }
        }
        
        /*
        public void stopHighlightLoops()
        {
            int i;
            
            highlightModeIsPlaying = false;
            if (actualNumberOfHighlightVoices > 0) // only if any ambient voices have been assigned to this channel
            {
                for (i = 0; i < actualNumberOfHighlightVoices; ++i)
                {
                    //highlightVoiceControl[i].stopPlayback(); // this stops abruptly
                    highlightVoiceControl[i].startMasterFadeOut(); // ramps master volume back down
                }
            }
        }
        
        public void stopBuildLoops()
        {
            int i;
            
            buildModeIsPlaying = false;
            if (actualNumberOfBuildVoices > 0) // only if any ambient voices have been assigned to this channel
            {
                for (i = 0; i < actualNumberOfBuildVoices; ++i)
                {
                    //buildVoiceControl[i].stopPlayback(); // this halts abruptly
                    buildVoiceControl[i].startMasterFadeOut(); // ramps master volume back down
                }
            }
        }*/

        
        public void stopAmbientLoops()
        {
            int i;
            
            ambientModeIsPlaying = false;
            if (actualNumberOfAmbientVoices[0] > 0) // only if any ambient voices have been assigned to this channel
            {
                for (i=0; i<actualNumberOfAmbientVoices[0]; ++i)
                    ambientVoiceControl[i, 0].stopPlayback();
            }

            if (actualNumberOfAmbientVoices[1] > 0)
            {
                for (i=0; i<actualNumberOfAmbientVoices[1]; ++i)
                    ambientVoiceControl[i, 1].stopPlayback();
            }
        }

        public void toggleAmbientPlayback() // this is for testing from keyboard
        {
            int i, j;
            
            if (!ambientModeIsPlaying)
            {
                ambientModeIsPlaying = true;
                currentlyActiveAmbientFile[0] = 0;
                currentlyActiveAmbientFile[1] = 0;

                // start both channels from the top:

                if (actualNumberOfAmbientVoices[0] > 0) // only if any ambient voices have been assigned to this channel
                    ambientVoiceControl[0, 0].startPlayback();
                if (actualNumberOfAmbientVoices[1] > 0)
                    ambientVoiceControl[0, 1].startPlayback();
            }
            else
            {
                ambientModeIsPlaying = false;
                if (actualNumberOfAmbientVoices[0] > 0) // only if any ambient voices have been assigned to this channel
                {
                    for (i=0; i<actualNumberOfAmbientVoices[0]; ++i)
                        ambientVoiceControl[i, 0].stopPlayback();
                }

                if (actualNumberOfAmbientVoices[1] > 0)
                {
                    for (i=0; i<actualNumberOfAmbientVoices[1]; ++i)
                        ambientVoiceControl[i, 1].stopPlayback();
                }
            }
        }

        private void ambientVoiceNearingEnd(int whichCh, int whichVoxID)
        {
            currentlyActiveAmbientFile[whichCh] += 1;
            if (currentlyActiveAmbientFile[whichCh] >= actualNumberOfAmbientVoices[whichCh])
            {
                currentlyActiveAmbientFile[whichCh] = 0; // start over with first voice
            }

            int newVoiceToPlay = currentlyActiveAmbientFile[whichCh];

            ambientVoiceControl[newVoiceToPlay, whichCh].startPlayback();
        }

        private void buildVoiceNearingEnd(int whichVoxID)
        {
            currentlyActiveBuildFile += 1;
            if (currentlyActiveBuildFile >= actualNumberOfBuildVoices)
            {
                currentlyActiveBuildFile = 0; // start over with first voice
            }

            int newVoiceToPlay = currentlyActiveBuildFile;

            buildVoiceControl[newVoiceToPlay].startPlayback();
        }

        private void highlightVoiceNearingEnd(int whichVoxID)
        {
            currentlyActiveHighlightFile += 1;
            if (currentlyActiveHighlightFile >= actualNumberOfHighlightVoices)
            {
                currentlyActiveHighlightFile = 0; // start over with first voice
            }

            int newVoiceToPlay = currentlyActiveHighlightFile;

            highlightVoiceControl[newVoiceToPlay].startPlayback();

        }




        private void rampUpHighlights()
        {
            for (int i = 0; i < actualNumberOfHighlightVoices; ++i)
            {
                highlightVoiceControl[i].startMasterFadeIn(); // this begins ramping up all highglight voice master volume
            }
        }
        
        private void rampDownHighlights()
        {
            for (int i = 0; i < actualNumberOfHighlightVoices; ++i)
            {
                highlightVoiceControl[i].startMasterFadeOut(); // this begins ramping down all highglight voice master volume
            }
        }

        private void rampUpBuilds()
        {
            for (int i = 0; i < actualNumberOfBuildVoices; ++i)
            {
                buildVoiceControl[i].startMasterFadeIn(); // this begins ramping up all build voice master volume
            }
        }
        
        private void rampDownBuilds()
        {
            for (int i = 0; i < actualNumberOfBuildVoices; ++i)
            {
                buildVoiceControl[i].startMasterFadeOut(); // this begins ramping down all build voice master volume
            }
        }


        private void listAvailableAudioDevices()
        {
            System.Collections.Generic.IList<string> listOfDev;
            
            listOfDev = AudioContext.AvailableDevices;

            actualNumberOfAudioDev = listOfDev.Count;
            if (actualNumberOfAudioDev > maxNumberOfAudioDev)
                actualNumberOfAudioDev = maxNumberOfAudioDev;
            
            for (int i = 0; i < actualNumberOfAudioDev; ++i)
            {
                audioDevList[i] = listOfDev[i];
            }
        }
        
        public string getAudioDevReport()
        {
            int i, channel;
            string timeFormatted;
            string valueToReturn = "AUDIO: number of avail dev = "+actualNumberOfAudioDev+ ": \n";
            for (i = 0; i < actualNumberOfAudioDev; ++i)
            {
                valueToReturn += "dev " + i + " [ " + audioDevList[i] + " ], \n";
            }

            valueToReturn += "dev in use: (" + audioDevIDEmployed + ") " + audioDevNameEmployed + "\n";
            valueToReturn += "\n";

            for (channel = 0; channel < 2; ++channel) // channels 0 and 1
            {
                actualNumberOfAmbientVoices[channel] = openAndReadXML.actualNumberOfAmbientLoopFilesPerChannel[myAppID, channel];
                for (i = 0; i < actualNumberOfAmbientVoices[channel]; ++i)
                {
                    timeFormatted = String.Format("{0:00.0}", ambientVoiceControl[i, channel].seqTimePassed);
                    valueToReturn += "track: " + i + " ch: " + channel + " VOL: " + ambientVoiceControl[i, channel].reportVolume + " state: " + ambientVoiceControl[i, channel].reportState;
                    valueToReturn += " time: "+ timeFormatted + " file: "+ambientVoiceControl[i, channel].audioFile + "\n";
                }
                valueToReturn += "\n"; // space between outputs
            }

            valueToReturn += "current STATE ( " + currentState +" ) \n ";

            valueToReturn += "build Voices:  \n ";
            for (i = 0; i < actualNumberOfBuildVoices; ++i)
            {
                timeFormatted = String.Format("{0:00.0}", buildVoiceControl[i].seqTimePassed);
                valueToReturn += "track: " + i + " ch: " + channel + " VOL: " + buildVoiceControl[i].reportVolume + " state: " + buildVoiceControl[i].reportState;
                valueToReturn += " time: "+ timeFormatted + " file: "+buildVoiceControl[i].audioFile + "\n";
            }

            valueToReturn += "highlight Voices: \n ";
            for (i = 0; i < actualNumberOfHighlightVoices; ++i)
            {
                timeFormatted = String.Format("{0:00.0}", highlightVoiceControl[i].seqTimePassed);
                valueToReturn += "track: " + i + " ch: " + channel + " VOL: " + highlightVoiceControl[i].reportVolume + " state: " + highlightVoiceControl[i].reportState;
                valueToReturn += " time: "+ timeFormatted + " file: "+highlightVoiceControl[i].audioFile + "\n";
            }

            return valueToReturn;
        }
        
        
        private void ambientVoice_OnEvent(object sender, AppEvent e)
        {
            int whichVoiceID;
            int whichChannel;
            
            if (e.EventSource == "ambient")
            {
                switch (e.EventString)
                {
                    case "prepareNextVoice":
                        whichChannel = Convert.ToInt32(e.EventArgs[0]);
                        whichVoiceID = Convert.ToInt32(e.EventArgs[1]);
                        ambientVoiceNearingEnd(whichChannel, whichVoiceID);
                        break;
                }
            }
        }

        private void buildVoice_OnEvent(object sender, AppEvent e)
        {
            int whichVoiceID;
            int whichChannel;
            
            if (e.EventSource == "build")
            {
                switch (e.EventString)
                {
                    case "prepareNextVoice":
                        //whichChannel = Convert.ToInt32(e.EventArgs[0]);
                        whichVoiceID = Convert.ToInt32(e.EventArgs[0]);
                        buildVoiceNearingEnd(whichVoiceID);
                        break;
                    case "loopStopped":
                        //whichChannel = Convert.ToInt32(e.EventArgs[0]);
                        whichVoiceID = Convert.ToInt32(e.EventArgs[0]);
                        buildLoopIsPlaying = false;
                        break;
                }
            }
        }

        private void highlightVoice_OnEvent(object sender, AppEvent e)
        {
            int whichVoiceID;
            int whichChannel;
            
            if (e.EventSource == "highlight")
            {
                switch (e.EventString)
                {
                    case "prepareNextVoice":
                        //whichChannel = Convert.ToInt32(e.EventArgs[0]);
                        whichVoiceID = Convert.ToInt32(e.EventArgs[0]);
                        highlightVoiceNearingEnd(whichVoiceID);
                        break;
                    case "loopStopped":
                        //whichChannel = Convert.ToInt32(e.EventArgs[0]);
                        whichVoiceID = Convert.ToInt32(e.EventArgs[0]);
                        highlightLoopIsPlaying = false;
                        break;
                }
            }

        }
        
        public void onClosing()
        {
            //stopSound();
            int i, j;

            // kill all ambient tracks:
            for (j = 0; j < 2; ++j) // channels 0 and 1
            {
                for (i = 0; i < actualNumberOfAmbientVoices[j]; ++i)
                {
                    ambientVoiceControl[i, j].onClosing();
                }
            }
        }
        
        
        /*
        private Thread soundThread;
        //public override void PlaySound(string assetName)
        private void PlaySound(string assetName)
        {
            doPlay = true;
            if (myAudioContext == null)
                myAudioContext = new AudioContext();
            
            soundThread = new Thread (() => this.PlaySoundThread (assetName, false));
            soundThread.Start ();
        }


        //public override void PlaySoundLoop (string assetName)
        private void PlaySoundLoop(string assetName)
        {
            doPlay = true;
            if (myAudioContext == null)
            {
                //myAudioContext = new AudioContext();
                //myAudioContext = new AudioContext(audioDevList[myID],48,30, true);
                myAudioContext = new AudioContext(audioDevList[myID]);
            }

            soundThread = new Thread (() => this.PlaySoundThread (assetName, true));
            
            soundThread.Start ();
        }

        private void stopSound()
        {
            // soundThread.Abort(); //this doesn't seem to do anything
            //ALSourceState.Stopped;
            doPlay = false;
        }
        */


        /*
        public void checkAudioDevAgain()
        {
            listAvailableAudioDevices();
        }
        public void toggleSoundPlayback()
        {
            if (!isPlayingSound)
            {
                if (myID == 1)
                    Thread.Sleep (300);
                isPlayingSound = true;
                PlaySoundLoop("na");
            }
            else
            {
                isPlayingSound = false;
                stopSound();
            }
        }
        

        // adopted from https://github.com/Insane96/aivengine-opentk/blob/master/aivengine-opentk/FastEngine.cs
        
        private void PlaySoundThread (string assetName, bool loop)
        {
            //string fileName = this.GetAsset (assetName).fileName;
            
            string fileName = "resources\\audioSamples\\Full_Engagment\\Full_Engagment_03_16bitPCM.wav";
            if (myID == 1)
                fileName = "resources\\audioSamples\\Ambient_Loops\\Mono_Ambient_Loop_22_16bitPCM.wav";
            
            
            string ext = fileName.Substring(fileName.LastIndexOf(@".") + 1);

            if (ext == "wav") {
                int channels, bits_per_sample, sample_rate;
                byte[] data = OpenTKAudioUtils.LoadWave (fileName, out channels, out bits_per_sample, out sample_rate);

                int buffer = AL.GenBuffer ();
                int source = AL.GenSource ();
                AL.BufferData (buffer, OpenTKAudioUtils.WaveFormat (channels, bits_per_sample), data, data.Length, sample_rate);

                AL.Source (source, ALSourcei.Buffer, buffer);
                AL.Source (source, ALSourceb.Looping, loop);
                
               // alSourcei(source, AL_SOURCE_RELATIVE, TRUE);
                //alSource3f(source, AL_POSITION, {pan, 0, -sqrtf(1.0f - pan*pan)});
                float panArg00 = 0.0f; // -0.5f is left | 0.5f is right
                if (myID == 1)
                    panArg00 = -0.5f;
                float panArg01 = 0.0f;
                float panArg02 = 0.0f - (float)Math.Sqrt(1.0f - (panArg00 * panArg00)); // should this be negative?
                
                AL.Source(source, ALSourcef.RolloffFactor, 0.0f);
                AL.Source(source, ALSourceb.SourceRelative, true);
                AL.Source(source, ALSource3f.Position, panArg00, panArg01, panArg02);

                AL.SourcePlay(source);

                int state;

                
                //do {
                //    Thread.Sleep (300);
                //    AL.GetSource (source, ALGetSourcei.SourceState, out state);
                //} while ((ALSourceState)state == ALSourceState.Playing);

                while (doPlay)
                {
                    Thread.Sleep (300);
                    AL.GetSource (source, ALGetSourcei.SourceState, out state);

                }
                
                AL.SourceStop (source);
                AL.DeleteSource (source);
                AL.DeleteBuffer (buffer);
            //} else if (ext == "ogg") {
             //   using (var streamer = new OggStreamer ()) {
             //       OggStream stream = new OggStream (fileName);
             //       stream.Prepare ();
             //       stream.Play ();
             //   }
            } else {
                throw new NotImplementedException($"Support for audio extension '{ext}' is not implemented.");
            }
        }*/
    }
}