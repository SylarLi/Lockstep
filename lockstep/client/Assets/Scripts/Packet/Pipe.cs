using System;
using System.IO;
using UnityEngine;

public class Pipe : IPipe
{
    public bool Parse(byte[] bytes)
    {
        bool result = true;
        try
        {
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            ms.Flush();
            BinaryReader br = new BinaryReader(ms);
            Parse(br);
            br.Close();
        }
        catch (Exception ecp)
        {
            Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
            result = false;
        }
        return result;
    }

    public byte[] ToBytes()
    {
        byte[] bytes = null;
        try
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            ToBytes(bw);
            bw.Flush();
            bytes = ms.ToArray();
            bw.Close();
        }
        catch (Exception ecp)
        {
            Debug.LogError(ecp.Message + "\n" + ecp.StackTrace);
        }
        return bytes;
    }

    public virtual void Parse(BinaryReader reader)
    {
        
    }

    public virtual void ToBytes(BinaryWriter writer)
    {
        
    }
}
