/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SimsLib.ThreeD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LogThis;

namespace TSOClient.ThreeD
{
    /// <summary>
    /// Represents a surface for rendering 3D elements (sims) on top of UI elements.
    /// </summary>
    public class UI3DView : ThreeDElement
    {
        private List<BasicEffect> m_Effects;

        private float m_Rotation;

        private List<Mesh> m_CurrentHeadMeshes;
        private List<Texture2D> m_HeadTextures;

        private RenderTarget2D m_RenderTarget;
        //private DepthStencilBuffer m_DSBuffer;

        private int m_Width, m_Height;
        private bool m_SingleRenderer = true;

        /// <summary>
        /// Constructs a new UI3DView instance. 
        /// </summary>
        /// <param name="Width">The width of this UI3DView surface.</param>
        /// <param name="Height">The height of this UI3DView surface.</param>
        /// <param name="SingleRenderer">Will this surface be used to render a single, or multiple sims?</param>
        /// <param name="Screen">The ThreeDScene instance with which to create this UI3DView instance.</param>
        /// <param name="StrID">The string ID for this UI3DView instance.</param>
        public UI3DView(int Width, int Height, bool SingleRenderer, ThreeDScene Screen, string StrID)
            : base(Screen)
        {
            m_Effects = new List<BasicEffect>();
            m_Width = Width;
            m_Height = Height;
            m_SingleRenderer = SingleRenderer;

            m_CurrentHeadMeshes = new List<Mesh>();
            m_HeadTextures = new List<Texture2D>();

            m_Scene.SceneMgr.Device.DeviceReset += new EventHandler<System.EventArgs>(GraphicsDevice_DeviceReset);
        }

        /// <summary>
        /// Occurs when the graphicsdevice was reset, meaning all 3D resources 
        /// have to be recreated.
        /// </summary>
        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            for (int i = 0; i < m_Effects.Count; i++)
                m_Effects[i] = new BasicEffect(m_Scene.SceneMgr.Device);

            /*m_Scene.SceneMgr.Device.VertexDeclaration = new VertexDeclaration(m_Scene.SceneMgr.Device,
                VertexPositionNormalTexture.VertexElements);*/
            m_Scene.SceneMgr.Device.RasterizerState = RasterizerState.CullNone;

            // Create camera and projection matrix
            m_Scene.SceneMgr.WorldMatrix = Matrix.Identity;
            m_Scene.SceneMgr.ViewMatrix = Matrix.CreateLookAt(Vector3.Right * 5, Vector3.Zero, Vector3.Down);
            m_Scene.SceneMgr.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f,
                    (float)m_Scene.SceneMgr.Device.PresentationParameters.BackBufferWidth /
                    (float)m_Scene.SceneMgr.Device.PresentationParameters.BackBufferHeight, 1.0f, 100.0f);
        }

        /// <summary>
        /// Loads a head mesh.
        /// </summary>
        /// <param name="MeshID">The ID of the mesh to load.</param>
        /// <param name="TexID">The ID of the texture to load.</param>
        public void LoadHeadMesh(Outfit Outf, int SkinColor)
        {
            Appearance Apr;

            switch (SkinColor)
            {
                case 0:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.LightAppearanceID));
                    break;
                case 1:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.MediumAppearanceID));
                    break;
                case 2:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.DarkAppearanceID));
                    break;
                default:
                    Apr = new Appearance(ContentManager.GetResourceFromLongID(Outf.LightAppearanceID));
                    break;
            }

            Binding Bnd = new Binding(ContentManager.GetResourceFromLongID(Apr.BindingIDs[0]));

            if (m_CurrentHeadMeshes.Count > 0)
            {
                if (!m_SingleRenderer)
                {
                    m_Effects.Add(new BasicEffect(m_Scene.SceneMgr.Device));
                    m_CurrentHeadMeshes.Add(new Mesh(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID), false));
                    m_CurrentHeadMeshes[m_CurrentHeadMeshes.Count - 1].ProcessMesh();

                    m_HeadTextures.Add(Texture2D.FromStream(m_Scene.SceneMgr.Device,
                        new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID))));
                }
                else
                {
                    m_Effects[0] = new BasicEffect(m_Scene.SceneMgr.Device);
                    m_CurrentHeadMeshes[0] = new Mesh(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID), false);
                    m_CurrentHeadMeshes[m_CurrentHeadMeshes.Count - 1].ProcessMesh();

                    m_HeadTextures[0] = Texture2D.FromStream(m_Scene.SceneMgr.Device,
                        new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID)));
                }
            }
            else
            {
                m_Effects.Add(new BasicEffect(m_Scene.SceneMgr.Device));
                m_CurrentHeadMeshes.Add(new Mesh(ContentManager.GetResourceFromLongID(Bnd.MeshAssetID), false));
                m_CurrentHeadMeshes[m_CurrentHeadMeshes.Count - 1].ProcessMesh();

                m_HeadTextures.Add(Texture2D.FromStream(m_Scene.SceneMgr.Device,
                    new MemoryStream(ContentManager.GetResourceFromLongID(Bnd.TextureAssetID))));
            }

            m_RenderTarget = CreateRenderTarget(m_Scene.SceneMgr.Device, 0, SurfaceFormat.Color);
            /*m_RenderTarget = new RenderTarget2D(m_Scene.SceneMgr.Device, 800, 600,
                1, SurfaceFormat.Color);*/

            //m_DSBuffer = CreateDepthStencil(m_RenderTarget);
        }

        public override void Update(GameTime GTime)
        {
            m_Rotation += 0.05f;
            m_Scene.SceneMgr.WorldMatrix = Matrix.CreateRotationX(m_Rotation);

            for (int i = 0; i < m_Effects.Count; i++)
            {
                for (int j = 0; j < m_HeadTextures.Count; j++)
                {
                    if (m_HeadTextures[j] != null)
                    {
                        //DepthStencilBuffer DSBuffer = m_Scene.SceneMgr.Device.DepthStencilBuffer;

                        m_Scene.SceneMgr.Device.SetRenderTarget(m_RenderTarget);
                        //m_Scene.SceneMgr.Device.DepthStencilBuffer = m_DSBuffer;

                        m_Scene.SceneMgr.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer,
                            Color.Transparent, 0, 0);

                        Viewport VPort = new Viewport();
                        VPort.Width = m_Width;
                        VPort.Height = m_Height;
                        m_Scene.SceneMgr.Device.Viewport = VPort;

                        m_Effects[i].World = m_Scene.SceneMgr.WorldMatrix;
                        m_Effects[i].View = m_Scene.SceneMgr.ViewMatrix;
                        m_Effects[i].Projection = m_Scene.SceneMgr.ProjectionMatrix;

                        m_Effects[i].Texture = m_HeadTextures[j];
                        m_Effects[i].TextureEnabled = true;

                        m_Effects[i].EnableDefaultLighting();

                        // Draw
                        //m_Effects[i].Begin();

                        for (int k = 0; k < m_Effects[i].Techniques.Count; k++)
                        {
                            foreach (EffectPass Pass in m_Effects[i].Techniques[k].Passes)
                            {
                                //Pass.Begin();
                                Pass.Apply();

                                foreach (Mesh Msh in m_CurrentHeadMeshes)
                                {
                                    foreach (Face Fce in Msh.Faces)
                                    {
                                        if (Msh.VertexTexNormalPositions != null)
                                        {
                                            VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[3];
                                            Vertex[0] = Msh.VertexTexNormalPositions[Fce.AVertexIndex];
                                            Vertex[1] = Msh.VertexTexNormalPositions[Fce.BVertexIndex];
                                            Vertex[2] = Msh.VertexTexNormalPositions[Fce.CVertexIndex];

                                            Vertex[0].TextureCoordinate = Msh.VertexTexNormalPositions[Fce.AVertexIndex].TextureCoordinate;
                                            Vertex[1].TextureCoordinate = Msh.VertexTexNormalPositions[Fce.BVertexIndex].TextureCoordinate;
                                            Vertex[2].TextureCoordinate = Msh.VertexTexNormalPositions[Fce.CVertexIndex].TextureCoordinate;

                                            m_Scene.SceneMgr.Device.DrawUserPrimitives<VertexPositionNormalTexture>(
                                                PrimitiveType.TriangleList, Vertex, 0, 1);
                                        }
                                    }
                                }

                                //Pass.End();
                                //m_Effects[i].End();

                                //Set the backbuffer as the rendertarget again!
                                m_Scene.SceneMgr.Device.SetRenderTarget(null);
                                //m_Scene.SceneMgr.Device.DepthStencilBuffer = DSBuffer;

                                /* m_SBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                                 m_SBatch.Draw(m_RenderTarget, new Rectangle(0, 0, 
                                     m_RenderTarget.Width, m_RenderTarget.Height), Color.White);

                                 m_SBatch.End();*/
                            }
                        }
                    }
                }
            }

            base.Update(GTime);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            if(m_RenderTarget != null)
                SBatch.Draw(m_RenderTarget, new Rectangle(0, 0, m_Width, m_Height), Color.White);
        }

        private RenderTarget2D CreateRenderTarget(GraphicsDevice device, int numberLevels, SurfaceFormat surface)
        {
            /*int width, height;

            // See if we can use our buffer size as our texture
            CheckTextureSize(device.PresentationParameters.BackBufferWidth,
                device.PresentationParameters.BackBufferHeight,
                out width, out height);*/

            // Create our render target
            return new RenderTarget2D(device,
                /*80*/m_Width, /*210*/m_Height, true, surface, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
        }
    }
}