using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    // This is taken entirely from https://stackoverflow.com/questions/6488034/how-to-implement-a-rule-engine
    public class Engine
    {
        static System.Linq.Expressions.Expression BuildExpr<T>(Expression r, ParameterExpression param)
        {
            var left = MemberExpression.Property(param, r.MemberName);
            var tProp = typeof(T).GetProperty(r.MemberName).PropertyType;
            ExpressionType tBinary;
            // is the operator a known .NET operator?
            if (ExpressionType.TryParse(r.Operator, out tBinary))
            {
                var right = System.Linq.Expressions.Expression.Constant(Convert.ChangeType(r.TargetValue, tProp));
                // use a binary operation, e.g. 'Equal' -> 'u.Age == 15'
                return System.Linq.Expressions.Expression.MakeBinary(tBinary, left, right);
            }

            if (r.Operator == "MatchRegex" || r.Operator == "NotMatchRegex")
            {
                var regex = new Regex(r.TargetValue);
                var method = typeof(Regex).GetMethod("IsMatch", new[] {typeof(string)});
                Debug.Assert(method != null, nameof(method) + " != null");
                var callInstance = System.Linq.Expressions.Expression.Constant(regex);

                var toStringMethod = tProp.GetMethod("ToString", new Type[0]);
                Debug.Assert(toStringMethod != null, nameof(toStringMethod) + " != null");
                var methodParam = System.Linq.Expressions.Expression.Call(left, toStringMethod);

                var call = System.Linq.Expressions.Expression.Call(callInstance, method, methodParam);
                if (r.Operator == "MatchRegex") return call;
                if (r.Operator == "NotMatchRegex") return System.Linq.Expressions.Expression.Not(call);
            }

            if (tProp.Name == "String")
            { 
                var method = tProp.GetMethod(r.Operator, new Type[] { typeof(string) });
                var tParam = method.GetParameters()[0].ParameterType;
                var right = System.Linq.Expressions.Expression.Constant(Convert.ChangeType(r.TargetValue, tParam));
                // use a method call, e.g. 'Contains' -> 'u.Tags.Contains(some_tag)'
                return System.Linq.Expressions.Expression.Call(left, method, right);
            }
            else { 
                var method = tProp.GetMethod(r.Operator);
                var tParam = method.GetParameters()[0].ParameterType;
                var right = System.Linq.Expressions.Expression.Constant(Convert.ChangeType(r.TargetValue, tParam));
                // use a method call, e.g. 'Contains' -> 'u.Tags.Contains(some_tag)'
                return System.Linq.Expressions.Expression.Call(left, method, right);
            }
        }

        public static Func<T, bool> CompileRule<T>(Expression r)
        {
            var paramUser = System.Linq.Expressions.Expression.Parameter(typeof(Operand));
            System.Linq.Expressions.Expression expr = BuildExpr<T>(r, paramUser);
            // build a lambda function User->bool and compile it
            var value = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile(true);
            return value;
        }

        public static List<ExpressionSet> FixRuleSets(List<ExpressionSet> rulesets)
        {
            foreach (var rules in rulesets)
            {
                FixRules(rules);
            }
            return rulesets;
        }

        public static ExpressionSet FixRules(ExpressionSet rules)
        {
            foreach (var rule in rules.Expressions)
            {
                if (rule.MemberName == "PremiereDate")
                {
                    var somedate = DateTime.Parse(rule.TargetValue);
                    rule.TargetValue = ConvertToUnixTimestamp(somedate).ToString();
                }
            }
            return rules;
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
    }	
}
