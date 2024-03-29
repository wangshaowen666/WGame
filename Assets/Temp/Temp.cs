using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class Temp 
{
   public static string ToJson(this object obj, int space = 0)
    {
        if (obj == null)
        {
            return "null";
        }
        else if (obj is string)
        {
            return "\"" + obj.ToString() + "\"";
        }
        else if (obj is Hashtable ht)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetSpace(space) + "{");
            if (ht.Count > 0)
            {
                foreach (var k in ht.Keys)
                {
                    sb.AppendLine(GetSpace(space + 4) + k.ToJson() + ":" + ht[k].ToJson(space + 4) + ",");
                }

                sb.Length -= 2;
                sb.AppendLine();
            }
            sb.AppendLine(GetSpace(space) + "}");
            return sb.ToString();
        }
        else if (obj is IDictionary dict)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetSpace(space) + "{");
            foreach (var k in dict.Keys)
            {
                sb.AppendLine(GetSpace(space + 4) + k.ToJson() + ":" + dict[k].ToJson(space + 4));
            }
            sb.AppendLine(GetSpace(space) + "}");
            return sb.ToString();
        }
        else if (obj is IList list)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetSpace(space) + "[");
            if (list.Count > 0)
            {
                foreach (var v in list)
                {
                    sb.AppendLine(GetSpace(space + 4) + v.ToJson() + ",");
                }
                sb.Length -= 2;
            }
            sb.AppendLine();
            sb.AppendLine(GetSpace(space) + "]");
            return sb.ToString();
        }
        else if (obj is IEnumerable)
        {
            StringBuilder sb = new StringBuilder();
            var ienumerable = (IEnumerable)obj;
            sb.AppendLine(GetSpace(space) + "[");
            foreach (var item in ienumerable)
            {
                sb.AppendLine(GetSpace(space + 4) + item.ToJson() + ",");
            }
            sb.AppendLine();
            sb.AppendLine(GetSpace(space) + "]");
            return sb.ToString();
        }
        else if (obj is DateTime)
        {
            return "\"" + obj.ToString() + "\"";
        }
        else
        {
            return obj.ToString();
        }
    }
   
   public static string GetSpace(int count)
   {
       StringBuilder sb = new StringBuilder();
       for (int i = 0; i < count; i++)
       {
           sb.Append(" ");
       }
       return sb.ToString();
   }
}
