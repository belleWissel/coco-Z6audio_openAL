using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace AudioControlApp
{
    class PPUtils
    {
    }


    public struct VertexPosTex
    {
        public Vector3 Position;
        public Vector2 Texture;

        public VertexPosTex(Vector3 position, Vector2 texture)
        {
            Position = position;
            Texture = texture;
        }

        public static readonly int Size = Vector3.SizeInBytes + Vector2.SizeInBytes;

        public static VertexPosTex[] GenerateFromData(float[] position, float[] texture)
        {
            VertexPosTex[] verts = new VertexPosTex[position.Length / 3];

            for (int i = 0; i < verts.Length; i++)
            {
                int j;

                j = (i + 1) * 3;
                verts[i].Position = new Vector3(position[j - 3],
                                                position[j - 2],
                                                position[j - 1]);

                j = (i + 1) * 2;
                verts[i].Texture = new Vector2(texture[j - 2],
                                               texture[j - 1]);
            }

            return verts;
        }
    }


    public struct VertexPosNoTex
    {
        public Vector3 Position;

        public VertexPosNoTex(Vector3 position)
        {
            Position = position;
            
        }

        public static readonly int Size = Vector3.SizeInBytes;

        public static VertexPosNoTex[] GenerateFromData(float[] position)
        {
            VertexPosNoTex[] verts = new VertexPosNoTex[position.Length / 3];

            for (int i = 0; i < verts.Length; i++)
            {
                int j;

                j = (i + 1) * 3;
                verts[i].Position = new Vector3(position[j - 3],
                                                position[j - 2],
                                                position[j - 1]);

                
            }

            return verts;
        }

        public static VertexPosNoTex[] GenerateFromData(Vector3[] position)
        {
            VertexPosNoTex[] verts = new VertexPosNoTex[position.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                int j;

                j = (i + 1) * 3;
                verts[i].Position = new Vector3(position[i].X,
                                                position[i].Y,
                                                position[i].Z);


            }

            return verts;
        }
    }


}
