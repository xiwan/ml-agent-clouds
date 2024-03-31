using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSTool.Array
{
    public static class ArrayTool 
    {
        public static string ToOneString(this ICollection collection)
        {
            var info = " ";
            foreach (var item in collection)
            {
                info += item.ToString() + " ";
            }
            return info;
        }
    }
}


