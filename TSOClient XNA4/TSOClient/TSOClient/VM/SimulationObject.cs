﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using SimsLib.IFF;

namespace TSOClient.VM
{
    [Serializable()]
    public class SimulationObject : ISerializable
    {
        private bool m_IsMultiTile = false;

        private float m_X, m_Y, m_Z;

        private OBJD m_MasterOBJD;
        private List<OBJD> m_Slaves = new List<OBJD>();
        private Iff m_ObjectContainer;
        private List<DrawGroupImg> m_Images = new List<DrawGroupImg>();
        protected string m_GUID; //Generated by the server when an object is placed on a lot.

        /// <summary>
        /// Is this a multi-tile object?
        /// If so, it has an OBJD for
        /// each tile.
        /// </summary>
        public bool IsMultiTileObject
        {
            get { return m_IsMultiTile; }
        }

        /// <summary>
        /// The master OBJD for this simulation object.
        /// Will contain the OBJD if this object is a 
        /// single-tile object.
        /// </summary>
        public OBJD Master
        {
            get { return m_MasterOBJD; }
        }

        /// <summary>
        /// The slave OBJDs for this object.
        /// Only applicable if the object is a
        /// multi-tile object.
        /// </summary>
        public List<OBJD> Slaves
        {
            get { return m_Slaves; }
        }

        /// <summary>
        /// The container archive for this simulation object.
        /// </summary>
        public Iff ObjectContainer
        {
            get { return m_ObjectContainer; }
        }

        /// <summary>
        /// The images for this object.
        /// If this object is a multi-tile object,
        /// there will be one image per tile.
        /// </summary>
        public List<DrawGroupImg> Images
        {
            get { return m_Images; }
        }

        public string GUID
        {
            get { return m_GUID; }
        }

        /// <summary>
        /// Creates a new SimulationObject instance.
        /// </summary>
        /// <param name="Obj">The OBJD for this object. Assumed to be the master OBJD if the object is multi-tile.</param>
        /// <param name="Container">The IFF archive where the OBJD resides.</param>
        public SimulationObject(OBJD Obj, Iff Container, string GUID)
        {
            m_GUID = GUID;

            if (!Obj.IsMultiTile)
            {
                m_MasterOBJD = Obj;
                m_ObjectContainer = Container;
            }
            else //Load the OBJDs for the other tiles...
            {
                foreach (OBJD O in m_ObjectContainer.OBJDs)
                {
                    if (O.MasterID == Obj.MasterID)
                        m_Slaves.Add(O);
                }
            }
        }

        public SimulationObject(string GUID)
        {
            m_GUID = GUID;
        }

        public void GetObjectData(SerializationInfo Info, StreamingContext Context)
        {
            Info.AddValue("X", m_X);
            Info.AddValue("Y", m_Y);
            Info.AddValue("Z", m_Z);
        }
    }
}
