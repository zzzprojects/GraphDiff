/*
 * This code is provided as is with no warranty. If you find a bug please report it on github.
 * If you would like to use the code please leave this comment at the top of the page
 * License MIT (c) Brent McKendrick 2012
 */

using System.Collections.Generic;
using RefactorThis.GraphDiff.Internal.Members;

namespace RefactorThis.GraphDiff.Internal
{
#warning move to RootEntity

    internal static class EntityFrameworkIncludeHelper
    {
        public static List<string> GetIncludeStrings(AMember root)
        {
            var list = new List<string>();

            if (root.Members.Count == 0 && root.IncludeString != null)
            {
                list.Add(root.IncludeString);
            }

            foreach (var member in root.Members)
            {
                list.AddRange(GetIncludeStrings(member));
            }
            return list;
        }
    }
}
