using System.Collections.Specialized;
using System.Web;
using XSP.Engine.Schema;
using XSP.Engine.Schema.Expressions;

namespace XSP.Engine
{
    public class XspRef
    {
        private readonly Dictionary<string, Tuple<string?, Node?>>? arguments;

        public readonly string? Script;
        public readonly string Sub;
        public readonly XspSource? Source;

        public string Full
        {
            get { return $"{Script ?? ""}#{Sub}"; }
        }

        private XspRef(string href, XspResolver resolver, string? pathContext, XspSource? source, XspScript? script = null)
        {
            Source = source;

            var qsIx = href.IndexOf('?');
            if (qsIx >= 0)
            {
                var nameValueCollection = HttpUtility.ParseQueryString(href[(qsIx + 1)..]);

                arguments = new();
                foreach (var key in nameValueCollection.Keys)
                {
                    if (key is string s)
                    {
                        var value = nameValueCollection[s];
                        if (value is string stringValue)
                        {
                            if (Expression.HasExpression(stringValue))
                            {
                                arguments[s] = new Tuple<string?, Node?>(null, Expression.Parse(stringValue));
                            }
                            else
                            {
                                arguments[s] = new Tuple<string?, Node?>(stringValue, null);
                            }
                        }
                    }
                }
                href = href[..qsIx];
            }

            var index = href.IndexOf('#');
            if (index == 0)
            {
                Script = null; // "#subname" -> no explicit script
                Sub = href[(index+1)..];
            }
            else if (index > 0)
            {
                // "scriptname#subname"
                var target = href[..index];
                if (target == "..")
                {
                    target = script?.BaseRef;
                    if (target == null)
                    {
                        throw new XspException(new XspError("Cannot use .. unless the script has a base", source));
                    }
                }
                else
                {
                    target = resolver.ResolveAndSimplify(target!, pathContext);
                }

                Script = target;
                Sub = href[(index+1)..];
            }
            else
            {
                // "scriptname"
                Script = resolver.ResolveAndSimplify(href, pathContext);
                Sub = "main";
            }
        }

        internal void CopyInto(XspScope scope, XspScope? parentScope)
        {
            if (arguments == null)
            {
                return;
            }

            foreach(var pair in arguments)
            {
                if (pair.Value.Item2 != null)
                {
                    scope.Set(pair.Key, pair.Value.Item2!.Evaluate(parentScope!));
                }
                else
                {
                    scope.Set(pair.Key, pair.Value.Item1!);
                }
            }
        }

        public static XspRef From(string href, XspResolver resolver, XspScript script, XspSource? source = null)
        {
            return new XspRef(href, resolver, script.ScriptName.Path, source, script);
        }

        public static XspRef From(string href, XspResolver resolver, string? pathContext = null, XspSource? source = null)
        {
            return new XspRef(href, resolver, pathContext, source);
        }
    }
}

