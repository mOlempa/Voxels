using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using UnityEngine;

public class SuccessorParser
{
    
    public static void ParseParamOperations(string[] tokens, ref Successor successor,
        int parametricSymbolOccurrenceIndex, out char[] paramNames)
    {
        //var indexedOperations = new Dictionary<int, List<Func<float, float>>>();
        List<char> parameterNamedInstances = new List<char>();
        if (tokens == null) {
            paramNames = parameterNamedInstances.ToArray();
            return;
        }

        Symbol s = successor.successorSymbols.Last();
        successor.successorSymbols.RemoveAt(successor.successorSymbols.Count - 1);
        s.parameters = new float[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(tokens[i])) continue;

            // Check if the token is just a value
            Match matchNumber = Regex.Match(tokens[i], @"(\d+)");
            if (matchNumber.Success)
            {
                int.TryParse(tokens[i], out int value);
                // Assign the value to the respective parameter
                s.parameters[i] = value;
            }

            // Find the single parameter name
            char? paramChar = tokens[i].FirstOrDefault(char.IsLetter);

            if (paramChar == null)
            {
                throw new ArgumentException($"Token '{tokens[i]}' does not contain a valid parameter name.");
            }

            char paramName = paramChar.Value;
            parameterNamedInstances.Add(paramName);
            //Debug.Log("token: " + token);

            try
            {
                // Create the parameter expression for the Lambda
                var parameter = Expression.Parameter(typeof(float), paramName.ToString());

                // Parse the string token into an expression tree
                var parser = new MathExpressionParser(tokens[i], paramName, parameter);
                Expression formulaBody = parser.Parse();

                // Compile the expression tree into a Func<float, float>
                var lambda = Expression.Lambda<Func<float, float>>(formulaBody, parameter);
                Func<float, float> compiledFunc = lambda.Compile();

                // Store or append to the indexedOperations dictionary
                if (!successor.indexedOperations.ContainsKey(parametricSymbolOccurrenceIndex))
                {
                    successor.indexedOperations[parametricSymbolOccurrenceIndex] = new List<Func<float, float>>();
                }
                successor.indexedOperations[parametricSymbolOccurrenceIndex].Add(compiledFunc);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to parse token expression: '{tokens[i]}'. Error: {ex.Message}", ex);
            }
        }
        paramNames = parameterNamedInstances.ToArray();
        successor.successorSymbols.Add(s);
        return;
    }

    // Inner helper class to parse the mathematical string grammar
    private class MathExpressionParser
    {
        private readonly string _expr;
        private readonly char _paramChar;
        private readonly ParameterExpression _paramExpr;
        private int _pos;

        public MathExpressionParser(string expr, char paramChar, ParameterExpression paramExpr)
        {
            _expr = expr.Replace(" ", "");
            _paramChar = paramChar;
            _paramExpr = paramExpr;
            _pos = 0;
        }

        public Expression Parse()
        {
            return ParseExpression();
        }

        // Addition and Subtraction
        private Expression ParseExpression()
        {
            Expression left = ParseTerm();
            while (_pos < _expr.Length)
            {
                char op = _expr[_pos];
                if (op == '+' || op == '-')
                {
                    _pos++;
                    Expression right = ParseTerm();
                    left = (op == '+') ? Expression.Add(left, right) : Expression.Subtract(left, right);
                }
                else break;
            }
            return left;
        }

        // Multiplication and Division
        private Expression ParseTerm()
        {
            Expression left = ParseFactor();
            while (_pos < _expr.Length)
            {
                char op = _expr[_pos];
                if (op == '*' || op == '/')
                {
                    _pos++;
                    Expression right = ParseFactor();
                    left = (op == '*') ? Expression.Multiply(left, right) : Expression.Divide(left, right);
                }
                else break;
            }
            return left;
        }

        // Exponents (^)
        private Expression ParseFactor()
        {
            Expression left = ParsePrimary();
            if (_pos < _expr.Length && _expr[_pos] == '^')
            {
                _pos++;
                Expression right = ParseFactor(); // Right-associative behavior for exponents

                // Math.Pow requires doubles, so cast up and down
                var powMethod = typeof(Math).GetMethod("Pow", new[] { typeof(double), typeof(double) });
                var castLeft = Expression.Convert(left, typeof(double));
                var castRight = Expression.Convert(right, typeof(double));
                var powCall = Expression.Call(powMethod, castLeft, castRight);

                left = Expression.Convert(powCall, typeof(float));
            }
            return left;
        }

        // Variables, constants, unary signs, and parentheses
        private Expression ParsePrimary()
        {
            if (_pos >= _expr.Length) throw new Exception("Unexpected end of expression.");

            // Parentheses
            if (_expr[_pos] == '(')
            {
                _pos++; // Consume '('
                Expression inner = ParseExpression();
                if (_pos >= _expr.Length || _expr[_pos] != ')') throw new Exception("Missing closing parenthesis.");
                _pos++; // Consume ')'
                return inner;
            }

            // Unary operators
            if (_expr[_pos] == '-')
            {
                _pos++;
                return Expression.Negate(ParsePrimary());
            }
            if (_expr[_pos] == '+')
            {
                _pos++;
                return ParsePrimary();
            }

            // Parameter Variable
            if (_expr[_pos] == _paramChar)
            {
                _pos++;
                return _paramExpr;
            }

            // Numeric Constants
            int start = _pos;
            while (_pos < _expr.Length && (char.IsDigit(_expr[_pos]) || _expr[_pos] == '.'))
            {
                _pos++;
            }

            if (start == _pos) throw new Exception($"Unexpected character '{_expr[_pos]}' encountered.");

            string numStr = _expr.Substring(start, _pos - start);
            float value = float.Parse(numStr, CultureInfo.InvariantCulture);
            return Expression.Constant(value);
        }
    }
}
