﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RTMP_LIB
{
    public enum AMFDataType { AMF_INVALID, AMF_NUMBER, AMF_BOOLEAN, AMF_STRING, AMF_OBJECT, AMF_NULL };

    public class AMFObject
    {
        List<AMFObjectProperty> m_properties = new List<AMFObjectProperty>();

        public AMFObject()
        {
            Reset();
        }

        public int Decode(byte[] pBuffer, int bufferOffset, int nSize, bool bDecodeName)
        {
            int nOriginalSize = nSize;
            bool bError = false; // if there is an error while decoding - try to at least find the end mark 0x000009

            while (nSize >= 3)
            {
                if (RTMP.ReadInt24(pBuffer, bufferOffset) == 0x00000009)
                {
                    nSize -= 3;
                    bError = false;
                    break;
                }

                if (bError)
                {
                    nSize--;
                    bufferOffset++;
                    continue;
                }

                AMFObjectProperty prop = new AMFObjectProperty();
                int nRes = prop.Decode(pBuffer, bufferOffset, nSize, bDecodeName);
                if (nRes == -1)
                    bError = true;
                else
                {
                    nSize -= nRes;
                    bufferOffset += nRes;
                    m_properties.Add(prop);
                }
            }

            if (bError) return -1;

            return nOriginalSize - nSize;
        }

        public int DecodeArray(byte[] buffer, int offset, int nSize, int nArrayLen, bool bDecodeName)
        {
            int nOriginalSize = nSize;
            bool bError = false;

            while (nArrayLen > 0)
            {
                nArrayLen--;

                AMFObjectProperty prop = new AMFObjectProperty();
                int nRes = prop.Decode(buffer, offset, nSize, bDecodeName);
                if (nRes == -1)
                    bError = true;
                else
                {
                    nSize -= nRes;
                    offset += nRes;
                    m_properties.Add(prop);
                }
            }
            if (bError)
                return -1;

            return nOriginalSize - nSize;
        }

        public void AddProperty(AMFObjectProperty prop)
        {
            m_properties.Add(prop);
        }

        public int GetPropertyCount()
        {
            return m_properties.Count;
        }

        public AMFObjectProperty GetProperty(string strName)
        {
            for (int n = 0; n < m_properties.Count; n++)
            {
                if (m_properties[n].GetPropName() == strName) return m_properties[n];
            }
            return null;
        }

        public AMFObjectProperty GetProperty(int nIndex)
        {
            if (nIndex >= m_properties.Count)
                return null;
            else
                return m_properties[nIndex];
        }

        public void FindMatchingProperty(string name, List<AMFObjectProperty> p, int stopAt)
        {
            for (int n = 0; n < GetPropertyCount(); n++)
            {
                AMFObjectProperty prop = GetProperty(n);

                if (prop.GetPropName() == name)
                {
                    if (p == null) p = new List<AMFObjectProperty>();
                    p.Add(GetProperty(n));
                    if (p.Count >= stopAt) return;
                }

                if (prop.GetDataType() == AMFDataType.AMF_OBJECT)
                {
                    prop.GetObject().FindMatchingProperty(name, p, stopAt);
                }
            }            
        }

        public void Dump()
        {
            for (int n = 0; n < m_properties.Count; n++) m_properties[n].Dump();
        }

        public void Reset()
        {
            m_properties.Clear();
        }

    }
}
