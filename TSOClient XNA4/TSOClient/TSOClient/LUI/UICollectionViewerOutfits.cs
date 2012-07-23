﻿/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Nicholas Roth. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SimsLib.FAR3;
using SimsLib.ThreeD;
using DNA;
using Microsoft.Xna.Framework;

namespace TSOClient.LUI
{
    /// <summary>
    /// A UICollectionViewer is a viewer to display browseable outfits (in the Create A Sim screen).
    /// </summary>
    public class UICollectionViewerOutfits : UIElement
    {
        private int myThumbSizeX, myThumbSizeY, myThumbMarginX, myThumbMarginY, myThumbImageSizeX, myThumbImageSizeY, myThumbImageOffsetX, myThumbImageOffsetY, myRows, myColumns;
        private ulong myMaleCollectionID;
        private ulong myFemaleCollectionID;
        private ulong myCurrentCollectionID;
        private UIScreen myScreen;
        private ScreenManager myScrMgr;
        private UIButton[,] myButtons;
        private UITextButton[] myTextButtons;
        private List<ulong> myPurchasables;
        private List<ulong> myOutfits;
        private List<ulong[]> myAppearances;
        private List<ulong[]> myThumbnails;
        private List<Texture2D> myCurrentThumbnails;
        private int mySkinColor;
        private int myPageStartIdx;
        private UILabel myCountLabel;
        private UIButton myLeftButton;
        private UIButton myRightButton;

        public int PageStartIdx { get { return myPageStartIdx; } set { myPageStartIdx = value; } }

        private Mesh m_CurrentBodyMesh;

        public UICollectionViewerOutfits(int x, int y, int thumbSizeX, int thumbSizeY, int thumbMarginX, int thumbMarginY, int thumbImageSizeX, int thumbImageSizeY, int thumbImageOffsetX, int thumbImageOffsetY, int rows, int columns, ulong maleCollectionID, ulong femaleCollectionID, UIScreen screen, string strID, ScreenManager scrnMgr)
            : base(screen, strID, DrawLevel.AlwaysOnTop)
        {
            myButtons = new UIButton[rows, columns];
            myScreen = screen;
            myScrMgr = scrnMgr;
            myMaleCollectionID = maleCollectionID;
            myFemaleCollectionID = femaleCollectionID;
            myCurrentCollectionID = femaleCollectionID;
            myThumbSizeX = thumbSizeX;
            myThumbSizeY = thumbSizeY;
            myThumbImageSizeX = thumbImageSizeX;
            myThumbImageSizeY = thumbImageSizeY;
            myThumbMarginX = thumbMarginX;
            myThumbMarginY = thumbMarginY;
            myThumbImageOffsetX = thumbImageOffsetX;
            myThumbImageOffsetY = thumbImageOffsetY;
            myPurchasables = new List<ulong>();
            myOutfits = new List<ulong>();
            myAppearances = new List<ulong[]>();
            myThumbnails = new List<ulong[]>();
            myCurrentThumbnails = null;
            myLeftButton = addButton(0x3f500000001, 415, 560, 1, false, strID + "LeftArrow");
            myRightButton = addButton(0x3f600000001, 650, 560, 1, false, strID + "RightArrow");

            /*myLeftButton.OnButtonClick += delegate(UIButton btn) { myPageStartIdx -= myRows * myColumns; myCurrentThumbnails = null; };
            myRightButton.OnButtonClick += delegate(UIButton btn) { myPageStartIdx += myRows * myColumns; myCurrentThumbnails = null; };*/

            myTextButtons = new UITextButton[12];
            for (int i = 0, stride = 0; i < 12; i++)
            {
                myTextButtons[i] = new UITextButton(455 + stride, 555, (i + 1).ToString(), strID + "NumberButton" + i, myScreen);
                myScreen.Add(myTextButtons[i]);
                myTextButtons[i].OnButtonClick += delegate(UIElement element) { myPageStartIdx = int.Parse(element.StrID.Substring(element.StrID.LastIndexOf("NumberButton") + 12)) * myRows * myColumns; myCurrentThumbnails = null; };
                if (i < 9)
                    stride += 15;
                else
                    stride += 22;
            }

            mySkinColor = 0;
            myRows = rows;
            myColumns = columns;
            myPageStartIdx = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    myButtons[i, j] = addButton(0x3df00000001, x + thumbMarginX + (j * (thumbMarginX + thumbSizeX)), y + thumbMarginY + (i * (thumbMarginY + thumbSizeY)), 1, false, strID + '_' + i + j);
                }
            }
            loadCollection();
            myCountLabel = new UILabel(0, strID + "CountLabel", 515, 537, myScreen);
            myCountLabel.Caption = "" + myThumbnails.Count + " Outfits";
            myScreen.Add(myCountLabel);
        }

        //Goes left in the collection of head-thumbnails.
        //Called from Lua (see "personselectionedit.lua").
        public void GoLeft()
        {
            myPageStartIdx -= myRows * myColumns;
            myCurrentThumbnails = null;
        }

        //Goes right in the collection of head-thumbnails.
        //Called from Lua (see "personselectionedit.lua").
        public void GoRight()
        {
            myPageStartIdx += myRows * myColumns;
            myCurrentThumbnails = null;
        }

        public void SetPage(int page)
        {
            myPageStartIdx = page * myRows * myColumns;
            myCurrentThumbnails = null;
        }

        public void SetGender(bool isMale)
        {
            if (isMale)
                myCurrentCollectionID = myMaleCollectionID;
            else
                myCurrentCollectionID = myFemaleCollectionID;
            myPurchasables = new List<ulong>();
            myOutfits = new List<ulong>();
            myAppearances = new List<ulong[]>();
            myThumbnails = new List<ulong[]>();
            myCurrentThumbnails = null;
            loadCollection();
            myCurrentThumbnails = null;
            myCountLabel.Caption = "" + myThumbnails.Count + " Outfits";
        }

        public void SetSkinColor(int skinColor)
        {
            mySkinColor = skinColor;
            myCurrentThumbnails = null;
        }

        /// <summary>
        /// Loads the collection with the ID that was passed in the constructor of this class.
        /// </summary>
        private void loadCollection()
        {
            BinaryReader br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(myCurrentCollectionID)));

            int count = Endian.SwapInt32(br.ReadInt32());
            for (int i = 0; i < count; i++)
            {
                br.ReadInt32();
                myPurchasables.Add(Endian.SwapUInt64(br.ReadUInt64()));
            }

            foreach (ulong purchasableID in myPurchasables)
            {
                br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(purchasableID)));

                br.BaseStream.Position = 16;
                byte[] outfitID = br.ReadBytes(8);
                ulong outfit = BitConverter.ToUInt64((byte[])outfitID.Reverse().ToArray(), 0);

                myOutfits.Add(outfit);
            }

            foreach (ulong outfitID in myOutfits)
            {
                br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(outfitID)));

                br.ReadUInt32();
                br.ReadUInt32();
                myAppearances.Add(new ulong[] { Endian.SwapUInt64(br.ReadUInt64()), Endian.SwapUInt64(br.ReadUInt64()), Endian.SwapUInt64(br.ReadUInt64()) });

            }

            foreach (ulong[] appearanceIDs in myAppearances)
            {
                ulong[] thumbnails = new ulong[3];
                for (int i = 0; i < 3; i++)
                {
                    br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(appearanceIDs[i])));

                    br.ReadInt32();

                    thumbnails[i] = Endian.SwapUInt64(br.ReadUInt64());

                }
                myThumbnails.Add(thumbnails);
            }
        }

        public override void Update(GameTime GTime)
        {
            if (myPageStartIdx == 0)
                myLeftButton.Disabled = true;
            else
                myLeftButton.Disabled = false;
            if (myPageStartIdx + myColumns * myRows >= myThumbnails.Count)
                myRightButton.Disabled = true;
            else
                myRightButton.Disabled = false;

            base.Update(GTime);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            int Scale = GlobalSettings.Default.ScaleFactor;

            bool regen = false;
            if (myCurrentThumbnails == null)
            {
                regen = true;
                myCurrentThumbnails = new List<Texture2D>();
            }
            for (int i = myPageStartIdx, r = 0; i < myPageStartIdx + myRows * myColumns && i < myThumbnails.Count; r++)
            {
                for (int j = 0; j < myColumns && i < myThumbnails.Count && i >= 0; j++, i++)
                {
                    Texture2D preview;
                    if (regen)
                    {
                        MemoryStream TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(myThumbnails[i][mySkinColor]));
                        preview = TextureConverter.BitmapToTexture(m_Screen.ScreenMgr.GraphicsDevice, new System.Drawing.Bitmap(TexStream));
                        myCurrentThumbnails.Add(preview);
                    }
                    else
                    {
                        preview = myCurrentThumbnails[r * myColumns + j];

                    }
                    SBatch.Draw(preview, new Rectangle(myButtons[r, j].X + myThumbImageOffsetX, myButtons[r, j].Y + myThumbImageOffsetY, myThumbSizeX - (myThumbImageOffsetX * 2), myThumbSizeY - (myThumbImageOffsetY * 2)),
                        new Rectangle(0, 0, myThumbImageSizeX * Scale, myThumbImageSizeY * Scale), Color.White);
                }
            }

            base.Draw(SBatch);
        }

        private UIButton addButton(ulong ID, int X, int Y, int Alpha, bool Enabled, string StrID)
        {
            MemoryStream TextureStream;
            Texture2D Texture;

            try
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = TextureConverter.BitmapToTexture(m_Screen.ScreenMgr.GraphicsDevice, 
                    new System.Drawing.Bitmap(TextureStream));
            }
            catch (FAR3Exception)
            {
                TextureStream = new MemoryStream(ContentManager.GetResourceFromLongID(ID));
                Texture = Texture = TextureConverter.BitmapToTexture(m_Screen.ScreenMgr.GraphicsDevice,
                    new System.Drawing.Bitmap(TextureStream));
                TextureStream.Close();
            }

            //Why did some genius at Maxis decide it was 'ok' to operate with three masking colors?!!
            if (Alpha == 1)
                ManualTextureMask(ref Texture, new Color(255, 0, 255));
            else if (Alpha == 2)
                ManualTextureMask(ref Texture, new Color(254, 2, 254));
            else if (Alpha == 3)
                ManualTextureMask(ref Texture, new Color(255, 1, 255));

            UIButton btn = new UIButton(X, Y, Texture, Enabled, StrID, myScreen);

            myScreen.Add(btn);

            return btn;
        }
    }
}
