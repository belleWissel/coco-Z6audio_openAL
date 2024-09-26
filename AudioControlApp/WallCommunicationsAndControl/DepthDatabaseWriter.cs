using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Timers;

//using System.ComponentModel;
using System.Threading;

/**************************

*/

namespace AudioControlApp.WallCommunicationsAndControl
{
    class DepthDataBaseWriter
    {

        public virtual void halt()
        {
        }

        public virtual void sendCombinedByteDataOverNetwork()
        {
        }

        public virtual bool attemptReconnect()
        {
            return false;
        }


        public virtual void collectDataForTransport(int i, int j, byte whichDepthData)
        {
        }

        public virtual void SendData(short[] data)
        {
        }

        public virtual void SendMeshData(byte[] package)
        {
            // pointPositions: linear array of point positions x,y,z, x,y,z, x,y,z....  length = numberOfValidPoints*3
            // pointUVs: linear array of point UV coordinates: x,y, x,y, x,y... length = numberOfValidPoints*2
            // whichMeshIndexList: linear array of triangles connect-the-dots: 1,3,4, 1,10,12, 3,4,7..... length = (total number of trangles) * 3
        }



    }
}
