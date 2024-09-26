using System;
using System.Configuration;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.IO;
//using System.Threading;
using SecondstoryCommon; // used for AppEventHandler
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using AudioControlApp.AnimationEngines;

namespace OpenTKAudio.Engine
{
    public class SingleVoiceControl
    {
        public event AppEventHandler OnEvent;
        private string myEventSource = "voice";
        
        private string audioDevName = "NA";

        private int myChannel = -1;
        private int myVoiceID = -1;

        private float panLevel = 0;
        private float playLength = 56.5f;
        private float myVolume = 0.5f;

        private bool isPlayingSound = false;
        private bool doPlay = false;

        public string audioFile = "resources\\audioSamples\\Ambient_Loops\\Mono_Ambient_Loop_22_16bitPCM.wav";

        //private Thread soundThread;
        private AudioContext myAudioContext; // this is shared across all voices!
        
        private int buffer;
        private int source;


        // ********************************************************
        // generic audio
        private System.Windows.Forms.Timer audioSeqTimer;

        private float audioOverlapTiming;
        private bool didSendEndApproachingFlag = false;

        // ambient specific

        public float seqTimePassed;
        private float seqTimeTick;
        
        //private float flagNextAmbientVoiceTime;
        //private float ambientOverlapTime = 0.0f;
        private bool isFadingOut = false;
        private float approxFileDuration = 0f;
        
        private int currentPlaybackState;
        public string reportState = "na";

        public float currentVolume = 0f;
        public int reportVolume = 0;
        public float maxVolume = 1.0f;
        
        
        // ********************************************************
        // build playback sequencer
        //private float buildOverlapTime = 2.0f;
        private float buildRampUpTime = 2.0f;
        private float buildRampDnTime = 2.0f;
        //private float flagNextBuildVoiceTime;
        
        // ********************************************************
        // highlight playback sequencer
        //private float highlightOverlapTime = 2.0f;
        private float highlightRampUpTime = 2.0f;
        private float highlightRampDnTime = 2.0f;
        //private float flagNextHighlightVoiceTime;
        
        // ********************************************************
        //
        private GeneralPurposeDisplayAnimation audioAnim;

        private float currentMasterVolume = 1.0f; // this is used to ramp very slowly up and down....
        private bool masterVolFadingOut = false;
        
        public SingleVoiceControl(int whichVoiceID, int whichChannel, bool isAmbient, bool isBuild, string whichFile, float whichLength)
        {
            audioAnim = new GeneralPurposeDisplayAnimation();
            
            myChannel = whichChannel;
            myVoiceID = whichVoiceID;

            audioFile = whichFile;
            if (whichChannel == 0)
                panLevel = -0.5f;
            else if (whichChannel == 1)
                panLevel = 0.5f;

            playLength = whichLength;

            if (isAmbient)
            {
                myEventSource = "ambient";
                audioAnim.setNewAlpha(1.0f, 15); // master volume always up for ambients
            }
            else if (isBuild)
            {
                myEventSource = "build";
                audioAnim.setNewAlpha(0.0f, 15); // master volume always dn for buildds (until mid event)
            }
            else
            {
                myEventSource = "highlight";
                audioAnim.setNewAlpha(0.0f, 15); // master volume always dn for highlights (until near event)
            }

            audioSeqTimer = new System.Windows.Forms.Timer();
            audioSeqTimer.Interval = 500;
            seqTimeTick = 0.5f;

            audioSeqTimer.Tick += new EventHandler(seuqenceTimer_Tick);
        }

        public void assignAmbientOverlapTime(float whichOverlap)
        {
            audioOverlapTiming = whichOverlap;
            //flagNextAmbientVoiceTime = playLength - whichOverlap;
            //flagNextAmbientVoiceTime = approxFileDuration - whichOverlap;
        }

        public void assignBuildOverlapTime(float whichOverlap)
        {
            audioOverlapTiming = whichOverlap;
            //flagNextBuildVoiceTime = playLength - whichOverlap;
        }

        public void assignBuildRampTime(float whichUp, float whichDn)
        {
            buildRampUpTime = whichUp;
            buildRampDnTime = whichDn;
        }
        
        public void assignHighlightOverlapTime(float whichOverlap)
        {
            audioOverlapTiming = whichOverlap;
            //flagNextHighlightVoiceTime = playLength - whichOverlap;
        }

        public void assignHighlightRampTime(float whichUp, float whichDn)
        {
            highlightRampUpTime = whichUp;
            highlightRampDnTime = whichDn;
        }

        public void assignMaxVolume(float whichVol)
        {
            maxVolume = whichVol;
        }

        
        public void update() // called with GL thread
        {
            audioAnim.update();

            if (audioAnim.value != currentVolume)
            {
                currentVolume = audioAnim.value;
                adjustVolumeTo(currentVolume*currentMasterVolume);
            }
            
            if (audioAnim.alpha != currentMasterVolume) // this is specific to build and highlight voices
            {
                currentMasterVolume = audioAnim.alpha;
                adjustVolumeTo(currentVolume*currentMasterVolume);

                if (masterVolFadingOut) 
                {
                    if (currentMasterVolume < 0.01f)
                    {
                        currentMasterVolume = 0f;
                        masterVolFadingOut = false;
                        stopPlayback();
                        doSendLoopStoppedCommand();
                    }
                }
            }
        }

        private void fadeVolumeUpToMax()
        {
            audioAnim.setNewValue(maxVolume, 100);
        }

        private void fadeVolumeOutFast()
        {
            audioAnim.setNewValue(0, 5);
        }

        private void fadeVolumeOut()
        {
            audioAnim.setNewValue(0, 120);
        }
        
        private void adjustVolumeTo(float whichVolume)
        {
            if (whichVolume < 0.01f)
                whichVolume = 0;
            
            reportVolume = (int)Math.Round(whichVolume* 100f); // used for HUD feedback
            
            if (source!=null)
                AL.Source(source, ALSourcef.Gain, whichVolume);
        }
        
        /*
        public void startBuildPlayback()
        {
            
            float whichPlaybackSpeed = 60.0f; // fps
            float totalBuildupFrames = whichPlaybackSpeed * buildRampUpTime;
            int buildFrames = (int) Math.Round(totalBuildupFrames);
            
            if (currentMasterVolume >= 0.01f) // there is already some volume present, shorten animation
            {
                totalBuildupFrames *= (1.0f - currentMasterVolume) / 1.0f;
                if (totalBuildupFrames < 30f) // but not too short
                    totalBuildupFrames = 30f;
                buildFrames = (int) Math.Round(totalBuildupFrames);
            }
            
            masterVolFadingOut = false;
            audioAnim.setNewAlpha(1.0f, buildFrames); // SLOWLY ramp UP
        }
        
        public void stopBuildPlayback()
        {
            float whichPlaybackSpeed = 60.0f; // fps
            float totalFadeOutFrames = whichPlaybackSpeed * buildRampDnTime;
            int fadeoutFrames = (int) Math.Round(totalFadeOutFrames);
            masterVolFadingOut = true;
            audioAnim.setNewAlpha(0.0f, fadeoutFrames); // SLOWLY ramp DOWN
        }
        
        public void startHighlightPlayback()
        {
            
            float whichPlaybackSpeed = 60.0f; // fps
            float totalBuildupFrames = whichPlaybackSpeed * highlightRampUpTime;
            int buildFrames = (int) Math.Round(totalBuildupFrames);
            
            if (currentMasterVolume >= 0.01f) // there is already some volume present, shorten animation
            {
                totalBuildupFrames *= (1.0f - currentMasterVolume) / 1.0f;
                if (totalBuildupFrames < 30f) // but not too short
                    totalBuildupFrames = 30f;
                buildFrames = (int) Math.Round(totalBuildupFrames);
            }

            masterVolFadingOut = false;
            audioAnim.setNewAlpha(1.0f, buildFrames); // SLOWLY ramp 
        }
        
        public void stopHighlightPlayback()
        {
            float whichPlaybackSpeed = 60.0f; // fps
            float totalFadeOutFrames = whichPlaybackSpeed * highlightRampDnTime;
            int fadeoutFrames = (int) Math.Round(totalFadeOutFrames);
            masterVolFadingOut = true;
            audioAnim.setNewAlpha(0.0f, fadeoutFrames); // SLOWLY down 
        }
        */
        
        public void startMasterFadeIn()
        {
            float whichPlaybackSpeed = 60.0f; // fps
            float totalBuildupFrames = whichPlaybackSpeed * highlightRampUpTime;
            if (myEventSource == "build")
                totalBuildupFrames = whichPlaybackSpeed * buildRampUpTime;
            int buildFrames = (int) Math.Round(totalBuildupFrames);
            
            if (currentMasterVolume >= 0.01f) // there is already some volume present, shorten animation
            {
                totalBuildupFrames *= (1.0f - currentMasterVolume) / 1.0f;
                if (totalBuildupFrames < 30f) // but not too short
                    totalBuildupFrames = 30f;
                buildFrames = (int) Math.Round(totalBuildupFrames);
            }

            masterVolFadingOut = false;
            audioAnim.setNewAlpha(1.0f, buildFrames); // SLOWLY ramp 
        }

        public void startMasterFadeOut()
        {
            float whichPlaybackSpeed = 60.0f; // fps
            float totalFadeOutFrames = whichPlaybackSpeed * highlightRampDnTime;
            if (myEventSource == "build")
                totalFadeOutFrames = whichPlaybackSpeed * buildRampDnTime;

            int fadeoutFrames = (int) Math.Round(totalFadeOutFrames);
            
            if (currentMasterVolume < 1.0f) // there is already some volume dropped, shorten animation
            {
                totalFadeOutFrames *= (currentMasterVolume) / 1.0f;
                if (totalFadeOutFrames < 30f) // but not too short
                    totalFadeOutFrames = 30f;
                fadeoutFrames = (int) Math.Round(totalFadeOutFrames);
            }

            masterVolFadingOut = true;
            audioAnim.setNewAlpha(0.0f, fadeoutFrames); // SLOWLY ramp DOWN
        }

        public void startPlayback()
        {
            if (!isPlayingSound)
            {
                isPlayingSound = true;
                PlaySound();
                seqTimePassed = 0;
                audioSeqTimer.Start();
                didSendEndApproachingFlag = false;
                isFadingOut = false;
                fadeVolumeUpToMax();
            }
        }

        public void stopPlayback()
        {
            if (isPlayingSound)
            {
                isPlayingSound = false;
                stopSound();
                audioSeqTimer.Stop();
                fadeVolumeOutFast();
            }
        }

        private void seuqenceTimer_Tick(object sender, EventArgs e)
        {
            seqTimePassed += seqTimeTick;

            //if (seqTimePassed > flagNextAmbientVoiceTime) 
            if (seqTimePassed > (approxFileDuration - audioOverlapTiming))
            {
                if (!didSendEndApproachingFlag) // if we already sent it...
                {
                    if (currentMasterVolume > 0.01f)
                    {
                        doSendNearingEndOfVoiceCommand();
                        didSendEndApproachingFlag = true;
                    }
                }
            }

            int state;
            AL.GetSource(source, ALGetSourcei.SourceState, out state);
            if ((ALSourceState)state == ALSourceState.Playing)
            {
                reportState = "playing";
            }
            else if ((ALSourceState)state == ALSourceState.Stopped)
            {
                reportState = "stopped";
            }

            if (seqTimePassed > approxFileDuration - 2f)
            {
                if (!isFadingOut)
                {
                    isFadingOut = true;
                    fadeVolumeOut();
                }
            }

            if (seqTimePassed > approxFileDuration + 0.5f)
            {
                stopPlayback();
            }
        }
        
        public void setAudioContext(AudioContext whichAudioContext)
        {
            myAudioContext = whichAudioContext;
            BufferData(); // wait until audio context is assigned before buffering
        }
        
        private void PlaySound()
        {
            AL.Source(source, ALSourceb.Looping, false);
            AL.SourcePlay(source);
        }

        private void PlaySoundLoop(string assetName)
        {
            AL.Source(source, ALSourceb.Looping, true);
            AL.SourcePlay(source);
        }

        private void stopSound()
        {
            AL.SourceStop(source);
        }


        private void BufferData()
        {

            buffer = AL.GenBuffer();
            source = AL.GenSource();

            //WaveData waveFile = new WaveData(Path.Combine(Path.Combine("..", "..", "Data", "Audio"), audioFile));
            WaveData waveFile = new WaveData(audioFile);

            AL.BufferData(buffer, waveFile.SoundFormat, waveFile.SoundData, waveFile.SoundData.Length, waveFile.SampleRate);
            //approxFileDuration = 0.5f * (float)waveFile.SoundData.Length / (float)waveFile.SampleRate;
            approxFileDuration = waveFile.durationInSec;
            waveFile.dispose();

            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.Source(source, ALSourceb.Looping, false);
            
            // arguments added to pan audio to one dir or the other
            float panArg00 = panLevel;
            float panArg01 = 0.0f;
            float panArg02 = 0.0f - (float)Math.Sqrt(1.0f - (panArg00 * panArg00)); // should this be negative?

            AL.Source(source, ALSourcef.RolloffFactor, 0.0f);
            AL.Source(source, ALSourceb.SourceRelative, true);
            AL.Source(source, ALSource3f.Position, panArg00, panArg01, panArg02);

            AL.GenSources(source);

            
            //float durationInSeconds = (float)waveFile.SoundData.Length / (float)waveFile.SampleRate;
            System.Diagnostics.Debug.WriteLine("[VOX] approx length = " + approxFileDuration);
        }
        

        private void doSendNearingEndOfVoiceCommand()
        { 
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Near Command #" + whichRegion);
            ArrayList argList = new ArrayList();
            if (myEventSource == "ambient")
            {
                argList.Add(myChannel);
                argList.Add(myVoiceID);
            }
            else
            {
                argList.Add(myVoiceID);
            }

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "prepareNextVoice";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }

        private void doSendLoopStoppedCommand()
        {
            //System.Diagnostics.Debug.WriteLine("[VIDEOSENSORGRID] sendActivate Near Command #" + whichRegion);
            ArrayList argList = new ArrayList();
            if (myEventSource == "ambient")
            {
                argList.Add(myChannel);
                argList.Add(myVoiceID);
            }
            else
            {
                argList.Add(myVoiceID);
            }

            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "loopStopped";
            evtData.EventArgs = argList;
            OnEvent(this, evtData);
        }


        public void onClosing()
        {
            AL.DeleteSource(source);
            AL.DeleteBuffer(buffer);
            
            stopSound();
            audioSeqTimer.Stop();
            audioSeqTimer.Dispose();
            
            //myAudioContext.Dispose();
        }


        /*
        private void PlaySoundThread(bool loop)
        {
            //string fileName = this.GetAsset (assetName).fileName;

            string fileName = audioFile;
            //if (myID == 1)
            //    fileName = "resources\\audioSamples\\Ambient_Loops\\Mono_Ambient_Loop_22_16bitPCM.wav";


            string ext = fileName.Substring(fileName.LastIndexOf(@".") + 1);

            if (ext == "wav")
            {
                int channels, bits_per_sample, sample_rate;
                byte[] data = OpenTKAudioUtils.LoadWave(fileName, out channels, out bits_per_sample, out sample_rate);

                int buffer = AL.GenBuffer();
                int source = AL.GenSource();
                AL.BufferData(buffer, OpenTKAudioUtils.WaveFormat(channels, bits_per_sample), data, data.Length, sample_rate);

                AL.Source(source, ALSourcei.Buffer, buffer);
                AL.Source(source, ALSourceb.Looping, loop);

                // alSourcei(source, AL_SOURCE_RELATIVE, TRUE);
                //alSource3f(source, AL_POSITION, {pan, 0, -sqrtf(1.0f - pan*pan)});
                //float panArg00 = 0.0f; // -0.5f is left | 0.5f is right
                //if (myID == 1)
                //    panArg00 = -0.5f;
                float panArg00 = panLevel;
                float panArg01 = 0.0f;
                float panArg02 = 0.0f - (float)Math.Sqrt(1.0f - (panArg00 * panArg00)); // should this be negative?

                AL.Source(source, ALSourcef.RolloffFactor, 0.0f);
                AL.Source(source, ALSourceb.SourceRelative, true);
                AL.Source(source, ALSource3f.Position, panArg00, panArg01, panArg02);

                AL.SourcePlay(source);

                int state;

                while (doPlay)
                {
                    //do
                    //{
                        Thread.Sleep(300);
                        AL.GetSource(source, ALGetSourcei.SourceState, out state);
                        currentPlaybackState = state;
                        //} while ((ALSourceState)state == ALSourceState.Playing);
                }

                ambientSeqTimer.Stop(); // we don't need this any more
                seqTimePassed = 0; // reset our timer
                
                AL.SourceStop(source);
                
                AL.GetSource(source, ALGetSourcei.SourceState, out state);
                currentPlaybackState = state;

                AL.DeleteSource(source);
                AL.DeleteBuffer(buffer);
            }
            else
            {
                throw new NotImplementedException($"Support for audio extension '{ext}' is not implemented.");
            }
        }*/

        
        /*
        // this is from https://gamedev.stackexchange.com/questions/71571/how-do-i-prevent-clicking-at-the-end-of-each-sound-play-in-openal
        public void BufferData(Stream audioDataStream)
        {
            if (audioDataStream == null)
            {
                throw new ArgumentNullException("audioDataStream");
            }

            using (BinaryReader reader = new BinaryReader(audioDataStream))
            {
                // RIFF File Marker
                string signature = new string(reader.ReadChars(4));
                
                if (signature != "RIFF")
                {
                    throw new NotSupportedException("Specified stream is not a wave file.");
                }

                //Size of the overall file
                reader.ReadInt32();

                // WAVE File Type Header
                string format = new string(reader.ReadChars(4));

                if (format != "WAVE")
                {
                    throw new NotSupportedException("Specified stream is not a wave file.");
                }

                // 'fmt ' Format chunk marker (Includes trailing null)
                string formatSignature = new string(reader.ReadChars(4));

                if (formatSignature != "fmt ")
                {
                    throw new NotSupportedException("Specified wave file is not supported.");
                }

                //Length of format data as listed above
                reader.ReadInt32();

                //Type of format (1 is PCM)
                reader.ReadInt16();

                //Number of Channels
                int channels = reader.ReadInt16();

                //Sample Rate
                int sampleRate = reader.ReadInt32();

                //(Sample Rate * BitsPerSample * Channels) / 8
                reader.ReadInt32();

                //(BitsPerSample * Channels) / 8
                reader.ReadInt16();

                //Bits per sample
                int bits = reader.ReadInt16();

                //"data" chunk header, Marks the beginning of the data section
                string dataSignature = new string(reader.ReadChars(4));

                if (dataSignature != "data")
                {
                    throw new NotSupportedException("Specified wave file is not supported.");
                }

                //Size of the data section
                int dataLength = reader.ReadInt32(); // <========== **The correct data length**

                ALFormat audioFormat = getAudioFormat(channels, bits);

                byte[] audioData = reader.ReadBytes(dataLength);

                //AL.BufferData(ID, audioFormat, audioData, audioData.Length, sampleRate);
            }
        }

        private ALFormat getAudioFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default: throw new NotSupportedException("The specified sound format is not supported.");
            }
        }*/
        
    }
}