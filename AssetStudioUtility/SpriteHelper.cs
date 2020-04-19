using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AssetStudio
{
    public static class SpriteHelper
    {
        static Bitmap BitmapFromPivot(Bitmap bmp, Vector2 pivot)
        {
            PointF offset=new PointF((pivot.X-0.5f)* bmp.Width,(pivot.Y-0.5f)* bmp.Height);
            Bitmap bmp2 = new Bitmap(bmp.Width+(int)(Math.Abs(offset.X)*2+0.5), bmp.Height + (int)(Math.Abs(offset.Y)*2 + 0.5));

            using (Graphics g = Graphics.FromImage(bmp2))
            {
                g.DrawImage(bmp, new RectangleF((bmp2.Width - bmp.Width) / 2 - offset.X, (bmp2.Height - bmp.Height) / 2 + offset.Y, bmp.Width, bmp.Height), new RectangleF(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
            }

            return bmp2;
        }
        public static Bitmap GetImage(this Sprite m_Sprite)
        {
            if (m_Sprite.m_SpriteAtlas != null && m_Sprite.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
            {
                if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(m_Sprite.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var m_Texture2D))
                {
                    Bitmap bmp = CutImage(m_Texture2D, m_Sprite, spriteAtlasData.textureRect, spriteAtlasData.textureRectOffset, spriteAtlasData.settingsRaw);
                    return bmp;
                }
            }
            else
            {
                if (m_Sprite.m_RD.texture.TryGet(out var m_Texture2D))
                {
                    Bitmap bmp= CutImage(m_Texture2D, m_Sprite, m_Sprite.m_RD.textureRect, m_Sprite.m_RD.textureRectOffset, m_Sprite.m_RD.settingsRaw);
                    //这里根据m_Sprite.m_Pivot来重新处理一下.
                    return bmp;
                }
            }
            return null;
        }

        private static Bitmap CutImage(Texture2D m_Texture2D, Sprite m_Sprite, RectangleF textureRect, Vector2 textureRectOffset, SpriteSettings settingsRaw)
        {
            Vector2 pivot=m_Sprite.m_Pivot;
            var version = m_Sprite.version;
            if (version[0] < 5
               || (version[0] == 5 && version[1] < 4)
               || (version[0] == 5 && version[1] == 4 && version[2] <= 1)) //5.4.1p3 down
            {
                pivot = new Vector2(0.5f, 0.5f);
            }

                var originalImage = m_Texture2D.ConvertToBitmap(true);
            if (originalImage != null)
            {
                //originalImage.Save("d:\\1.png");
                using (originalImage)
                {
                    var spriteImage = new Bitmap((int)m_Sprite.m_Rect.Width, (int)m_Sprite.m_Rect.Height, PixelFormat.Format32bppArgb);
                    var srcRect = new RectangleF(m_Sprite.m_Rect.X, (m_Texture2D.m_Height - m_Sprite.m_Rect.Y - m_Sprite.m_Rect.Height), m_Sprite.m_Rect.Width, m_Sprite.m_Rect.Height);
                    var dstRect = new RectangleF(0, 0, m_Sprite.m_Rect.Width, m_Sprite.m_Rect.Height);
                    using (var g = Graphics.FromImage(spriteImage))
                    {
                        g.DrawImage(originalImage, dstRect, srcRect, GraphicsUnit.Pixel);
                    }

                    if(settingsRaw.packed == 1)
                    {//这里与旋转相关,需要重新调查一下!
                        switch (settingsRaw.packingRotation)
                        {
                            case SpritePackingRotation.kSPRFlipHorizontal:
                                spriteImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                break;
                            case SpritePackingRotation.kSPRFlipVertical:
                                spriteImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                                break;
                            case SpritePackingRotation.kSPRRotate180:
                                spriteImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                break;
                            case SpritePackingRotation.kSPRRotate90:
                                spriteImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                break;
                        }
                    }
                    if (settingsRaw.packingMode == SpritePackingMode.kSPMTight)
                    {

                        //这里明显使用到了多边形!!
                        try
                        {
                            var triangles = GetTriangles(m_Sprite.m_RD);
                            List<Point> point_list = new List<Point>();
                            //找到最小的x,找到最小的y
                            if (true)
                            {
                                List<Vector2> point_list_0 = new List<Vector2>();
                                for (int i = 0; i < triangles.Length; i++)
                                {
                                    point_list_0.Add(triangles[i][0]);
                                    point_list_0.Add(triangles[i][1]);
                                    point_list_0.Add(triangles[i][2]);
                                }

                                float minx = 100000000;
                                float miny = 100000000;
                                foreach(Vector2 v in point_list_0)
                                {
                                    if (v.X < minx) minx = v.X;
                                    if (v.Y < miny) miny = v.Y;
                                }
                                foreach (Vector2 v in point_list_0)
                                {
                                    int nx = (int)((v.X - minx) * m_Sprite.m_PixelsToUnits + 0.5f);
                                    int ny = (int)((v.Y - miny) * m_Sprite.m_PixelsToUnits + 0.5f);
                                    point_list.Add(new Point(nx, spriteImage.Height - ny));
                                }

                            }
                            var graphic_path = new GraphicsPath();
                            if (true)
                            {
                                for (int i = 0; i < point_list.Count / 3; i++)
                                {
                                    List<Point> vector_list_2 = new List<Point>();
                                    vector_list_2.Add(point_list[i * 3 + 0]);
                                    vector_list_2.Add(point_list[i * 3 + 1]);
                                    vector_list_2.Add(point_list[i * 3 + 2]);
                                    graphic_path.AddPolygon(vector_list_2.ToArray());
                                }
                            }
                           

                            Bitmap bmp1 = new Bitmap(spriteImage.Width, spriteImage.Height);
                            using (Graphics g = Graphics.FromImage(bmp1))
                            {
                                var brush = new TextureBrush(spriteImage);
                                g.FillPath(brush, graphic_path);
                            }

                            Bitmap bmp3 = BitmapFromPivot(bmp1, pivot);
                            return bmp3;
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    Bitmap bmp2 = BitmapFromPivot(spriteImage, pivot);
                    return bmp2;
                }
            }
            return null;
        }


        private static Vector2[][] GetTriangles(SpriteRenderData m_RD)
        {
            if (m_RD.vertices != null) //5.6 down
            {
                var vertices = m_RD.vertices.Select(x => (Vector2)x.pos).ToArray();
                var triangleCount = m_RD.indices.Length / 3;
                var triangles = new Vector2[triangleCount][];
                for (int i = 0; i < triangleCount; i++)
                {
                    var first = m_RD.indices[i * 3];
                    var second = m_RD.indices[i * 3 + 1];
                    var third = m_RD.indices[i * 3 + 2];
                    var triangle = new[] { vertices[first], vertices[second], vertices[third] };
                    triangles[i] = triangle;
                }
                return triangles;
            }

            return GetTriangles(m_RD.m_VertexData, m_RD.m_SubMeshes, m_RD.m_IndexBuffer); //5.6 and up
        }

        private static Vector2[][] GetTriangles(VertexData m_VertexData, SubMesh[] m_SubMeshes, byte[] m_IndexBuffer)
        {
            var triangles = new List<Vector2[]>();
            var m_Channel = m_VertexData.m_Channels[0]; //kShaderChannelVertex
            var m_Stream = m_VertexData.m_Streams[m_Channel.stream];
            using (BinaryReader vertexReader = new BinaryReader(new MemoryStream(m_VertexData.m_DataSize)),
                                indexReader = new BinaryReader(new MemoryStream(m_IndexBuffer)))
            {
                foreach (var subMesh in m_SubMeshes)
                {
                    vertexReader.BaseStream.Position = m_Stream.offset + subMesh.firstVertex * m_Stream.stride + m_Channel.offset;

                    var vertices = new Vector2[subMesh.vertexCount];
                    for (int v = 0; v < subMesh.vertexCount; v++)
                    {
                        vertices[v] = vertexReader.ReadVector3();
                        vertexReader.BaseStream.Position += m_Stream.stride - 12;
                    }

                    indexReader.BaseStream.Position = subMesh.firstByte;

                    var triangleCount = subMesh.indexCount / 3u;
                    for (int i = 0; i < triangleCount; i++)
                    {
                        var first = indexReader.ReadUInt16() - subMesh.firstVertex;
                        var second = indexReader.ReadUInt16() - subMesh.firstVertex;
                        var third = indexReader.ReadUInt16() - subMesh.firstVertex;
                        var triangle = new[] { vertices[first], vertices[second], vertices[third] };
                        triangles.Add(triangle);
                    }
                }
            }
            return triangles.ToArray();
        }
    }
}
