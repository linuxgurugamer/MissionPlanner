#if false
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MissionPlanner.Utils
{
}


namespace MissionPlanner.Utils.MathExpressionEvaluator
{
    /// <summary>
    /// Variable resolver:
    ///  - name: variable name (e.g., "twr").
    ///  - arg:  null (no brackets), double (numeric arg), or string (string arg).
    /// </summary>
    public delegate double VariableResolver(string name, object arg);

    /// <summary>
    /// Variable setter for assignments: name = value.
    /// </summary>
    public delegate void VariableSetter(string name, double value);

    /// <summary>
    /// Expression evaluator with:
    ///  - +, -, *, /, ^
    ///  - parentheses
    ///  - unary +/-
    ///  - variables and assignment (x = 3 + 4)
    ///  - bracket arguments: var[expr] or var["text"]
    ///  - functions: sin, cos, tan, asin, acos, atan, sqrt, abs, ln, log, powerof, pow
    /// </summary>
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// Evaluate expression without variables.
        /// </summary>
        public static double Evaluate(string expression)
        {
            return Evaluate(expression, null, null);
        }

        /// <summary>
        /// Evaluate expression with variable resolver (no assignment).
        /// </summary>
        public static double Evaluate(string expression, VariableResolver variableResolver)
        {
            return Evaluate(expression, variableResolver, null);
        }

        /// <summary>
        /// Evaluate expression with variables and assignments.
        /// </summary>
        /// <param name="expression">Expression string.</param>
        /// <param name="variableResolver">
        ///   Called for variable reads: resolver("twr", null), resolver("twr", 2.0), resolver("twr", "launch"), etc.
        /// </param>
        /// <param name="variableSetter">
        ///   Called for assignments: setter("x", 7.0) for "x = 3 + 4".
        /// </param>
        public static double Evaluate(string expression, VariableResolver variableResolver, VariableSetter variableSetter)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var parser = new Parser(expression, variableResolver, variableSetter);
            double result = parser.ParseRoot();

            parser.SkipWhitespace();
            if (!parser.EndOfInput)
                throw new FormatException(
                    $"Unexpected character at position {parser.Position}: '{parser.CurrentChar}'");

            return result;
        }

        /// <summary>
        /// Internal recursive-descent parser.
        /// Grammar (top level):
        ///   root       := assignment
        ///   assignment := identifier '=' expression | expression
        ///
        ///   expression := term (('+' | '-') term)*
        ///   term       := factor (('*' | '/') factor)*
        ///   factor     := unary ('^' unary)*
        ///   unary      := ('+' | '-') unary | primary
        ///   primary    := number
        ///              | variable
        ///              | functionCall
        ///              | '(' expression ')'
        ///
        ///   variable      := identifier ('[' (stringLiteral | expression) ']')?
        ///   functionCall  := identifier '(' [ expression (',' expression)* ] ')'
        /// </summary>
        private sealed class Parser
        {
            private readonly string _text;
            private readonly VariableResolver _variableResolver;
            private readonly VariableSetter _variableSetter;
            private int _pos;

            public Parser(string text, VariableResolver variableResolver, VariableSetter variableSetter)
            {
                _text = text;
                _variableResolver = variableResolver;
                _variableSetter = variableSetter;
                _pos = 0;
            }

            public int Position => _pos;
            public bool EndOfInput => _pos >= _text.Length;
            public char CurrentChar => EndOfInput ? '\0' : _text[_pos];

            public void SkipWhitespace()
            {
                while (!EndOfInput && char.IsWhiteSpace(CurrentChar))
                    _pos++;
            }

            public bool Match(char c)
            {
                SkipWhitespace();
                if (!EndOfInput && CurrentChar == c)
                {
                    _pos++;
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Entry point: handle assignment at top level.
            /// </summary>
            public double ParseRoot()
            {
                return ParseAssignment();
            }

            /// <summary>
            /// assignment := identifier '=' expression | expression
            /// Only the top-level root supports assignment in this implementation.
            /// </summary>
            private double ParseAssignment()
            {
                SkipWhitespace();
                int startPos = _pos;

                // Look ahead for identifier '='
                if (!EndOfInput && (char.IsLetter(CurrentChar) || CurrentChar == '_'))
                {
                    int idStart = _pos;
                    string name = ParseIdentifier();
                    SkipWhitespace();

                    if (!EndOfInput && CurrentChar == '=')
                    {
                        // It's an assignment
                        _pos++; // consume '='

                        if (_variableSetter == null)
                            throw new InvalidOperationException(
                                $"Assignment to '{name}' but no VariableSetter was provided.");

                        double value = ParseExpression();
                        _variableSetter(name, value);
                        return value;
                    }

                    // Not an assignment: roll back and parse as normal expression
                    _pos = startPos;
                }

                return ParseExpression();
            }

            // expression := term (('+' | '-') term)*
            public double ParseExpression()
            {
                double value = ParseTerm();

                while (true)
                {
                    SkipWhitespace();

                    if (Match('+'))
                    {
                        double rhs = ParseTerm();
                        value += rhs;
                    }
                    else if (Match('-'))
                    {
                        double rhs = ParseTerm();
                        value -= rhs;
                    }
                    else
                    {
                        break;
                    }
                }

                return value;
            }

            // term := factor (('*' | '/') factor)*
            private double ParseTerm()
            {
                double value = ParseFactor();

                while (true)
                {
                    SkipWhitespace();

                    if (Match('*'))
                    {
                        double rhs = ParseFactor();
                        value *= rhs;
                    }
                    else if (Match('/'))
                    {
                        double rhs = ParseFactor();
                        value /= rhs;
                    }
                    else
                    {
                        break;
                    }
                }

                return value;
            }

            // factor := unary ('^' unary)*
            private double ParseFactor()
            {
                // ^ is right-associative: 2^3^2 = 2^(3^2)
                double value = ParseUnary();

                while (true)
                {
                    SkipWhitespace();
                    if (Match('^'))
                    {
                        double exponent = ParseUnary();
                        value = Math.Pow(value, exponent);
                    }
                    else
                    {
                        break;
                    }
                }

                return value;
            }

            // unary := ('+' | '-') unary | primary
            private double ParseUnary()
            {
                SkipWhitespace();

                if (Match('+'))
                    return ParseUnary();

                if (Match('-'))
                    return -ParseUnary();

                return ParsePrimary();
            }

            // primary := number | variable | functionCall | '(' expression ')'
            private double ParsePrimary()
            {
                SkipWhitespace();

                if (Match('('))
                {
                    double value = ParseExpression();
                    SkipWhitespace();
                    if (!Match(')'))
                        throw new FormatException($"Missing closing parenthesis at position {Position}");
                    return value;
                }

                if (!EndOfInput)
                {
                    char c = CurrentChar;

                    // Identifier-based: variable or function
                    if (char.IsLetter(c) || c == '_')
                    {
                        return ParseIdentifierPrimary();
                    }

                    // Number
                    if (char.IsDigit(c) || c == '.')
                    {
                        return ParseNumber();
                    }
                }

                throw new FormatException($"Unexpected character at position {Position}: '{CurrentChar}'");
            }

            /// <summary>
            /// Handle identifier-based primary: either function call or variable access.
            /// </summary>
            private double ParseIdentifierPrimary()
            {
                string name = ParseIdentifier();
                SkipWhitespace();

                // Function call: name '(' ...
                if (Match('('))
                {
                    var args = new List<double>();
                    SkipWhitespace();

                    if (!Match(')'))
                    {
                        while (true)
                        {
                            double arg = ParseExpression();
                            args.Add(arg);
                            SkipWhitespace();

                            if (Match(')'))
                                break;

                            if (!Match(','))
                                throw new FormatException(
                                    $"Expected ',' or ')' in argument list for function '{name}' at position {Position}");
                        }
                    }

                    return EvaluateFunction(name, args.ToArray());
                }

                // Variable with optional bracket argument: name '[' (stringLiteral | expression) ']'
                object bracketArg = null;
                if (Match('['))
                {
                    bracketArg = ParseBracketArgument();
                }

                if (_variableResolver == null)
                {
                    throw new InvalidOperationException(
                        $"Variable '{name}' used but no VariableResolver was provided.");
                }

                return _variableResolver(name, bracketArg);
            }

            /// <summary>
            /// Parse identifier: [letter|_] [letter|digit|_]*
            /// </summary>
            private string ParseIdentifier()
            {
                SkipWhitespace();
                if (EndOfInput || !(char.IsLetter(CurrentChar) || CurrentChar == '_'))
                    throw new FormatException($"Identifier expected at position {Position}");

                int start = _pos;
                _pos++; // first char

                while (!EndOfInput)
                {
                    char c = CurrentChar;
                    if (char.IsLetterOrDigit(c) || c == '_')
                        _pos++;
                    else
                        break;
                }

                return _text.Substring(start, _pos - start);
            }

            /// <summary>
            /// Parse inside of '[' ... ']' and return:
            ///  - string for "..." or '...'
            ///  - double for numeric expression.
            /// </summary>
            private object ParseBracketArgument()
            {
                SkipWhitespace();

                // String literal: "text" or 'text'
                if (!EndOfInput && (CurrentChar == '"' || CurrentChar == '\''))
                {
                    string s = ParseStringLiteral();
                    SkipWhitespace();
                    if (!Match(']'))
                        throw new FormatException(
                            $"Missing closing ']' for bracket argument at position {Position}");
                    return s;
                }

                // Otherwise a numeric expression
                double value = ParseExpression();
                SkipWhitespace();
                if (!Match(']'))
                    throw new FormatException(
                        $"Missing closing ']' for bracket argument at position {Position}");
                return value;
            }

            /// <summary>
            /// Simple string literal parser: "text" or 'text', no escape handling.
            /// </summary>
            private string ParseStringLiteral()
            {
                SkipWhitespace();
                if (EndOfInput || (CurrentChar != '"' && CurrentChar != '\''))
                    throw new FormatException($"String literal expected at position {Position}");

                char quote = CurrentChar;
                _pos++; // skip opening quote
                int start = _pos;

                while (!EndOfInput && CurrentChar != quote)
                    _pos++;

                if (EndOfInput)
                    throw new FormatException("Unterminated string literal");

                string s = _text.Substring(start, _pos - start);
                _pos++; // skip closing quote
                return s;
            }

            /// <summary>
            /// Parse a number: [digits][.digits][exponent]
            /// </summary>
            private double ParseNumber()
            {
                SkipWhitespace();

                int start = _pos;
                bool hasDigits = false;

                // Integer/decimal part
                while (!EndOfInput && (char.IsDigit(CurrentChar) || CurrentChar == '.'))
                {
                    hasDigits = true;
                    _pos++;
                }

                // Optional exponent: e[+/-]?digits
                if (!EndOfInput && (CurrentChar == 'e' || CurrentChar == 'E'))
                {
                    int expPos = _pos;
                    _pos++; // 'e' or 'E'

                    if (!EndOfInput && (CurrentChar == '+' || CurrentChar == '-'))
                        _pos++;

                    bool hasExpDigits = false;
                    while (!EndOfInput && char.IsDigit(CurrentChar))
                    {
                        hasExpDigits = true;
                        _pos++;
                    }

                    if (!hasExpDigits)
                    {
                        // Malformed exponent; roll back
                        _pos = expPos;
                    }
                }

                if (!hasDigits)
                    throw new FormatException($"Number expected at position {start}");

                string token = _text.Substring(start, _pos - start);

                double value;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw new FormatException($"Invalid number '{token}' at position {start}");

                return value;
            }

            /// <summary>
            /// Built-in functions: sin, cos, tan, asin, acos, atan, sqrt, abs, ln, log, powerof, pow.
            /// Arguments are in radians for trig.
            /// </summary>
            private double EvaluateFunction(string name, double[] args)
            {
                string key = name.ToLowerInvariant();

                switch (key)
                {
                    case "abs":
                        RequireArgCount(name, args, 1);
                        return Math.Abs(args[0]);

                    case "acos":
                        RequireArgCount(name, args, 1);
                        return Math.Acos(args[0]);

                    case "asin":
                        RequireArgCount(name, args, 1);
                        return Math.Asin(args[0]);

                    case "atan":
                        RequireArgCount(name, args, 1);
                        return Math.Atan(args[0]);

                    case "ceiling":
                        RequireArgCount(name, args, 1);
                        return Math.Ceiling(args[0]);

                    case "clamp":
                        RequireArgCount(name, args, 3);
                        return Mathf.Clamp((float)args[0], (float)args[1], (float)args[2]);

                    case "floor":
                        RequireArgCount(name, args, 1);
                        return Math.Floor(args[0]);

                    case "cos":
                        RequireArgCount(name, args, 1);
                        return Math.Cos(args[0]);

                    case "cosh":
                        // provide the hyperbolic cosine value of the specified angle.
                        RequireArgCount(name, args, 1);
                        return Math.Cosh(args[0]);

                    case "ln":
                        // Natural log
                        RequireArgCount(name, args, 1);
                        return Math.Log(args[0]);

                    case "log":
                        // Base-10 log
                        RequireArgCount(name, args, 1);
                        return Math.Log10(args[0]);

                    case "max":
                        RequireArgCount(name, args, 2);
                        return Math.Max(args[0], args[1]);

                    case "min":
                        RequireArgCount(name, args, 2);
                        return Math.Min(args[0], args[1]);

                    case "round":
                        RequireArgCount(name, args, 1);
                        return Math.Round(args[0]);

                    case "powerof":
                    case "pow":
                        RequireArgCount(name, args, 2);
                        return Math.Pow(args[0], args[1]);

                    case "sin":
                        RequireArgCount(name, args, 1);
                        return Math.Sin(args[0]);

                    case "sinh":
                        // provide the hyperbolic sine value of the specified angle.
                        RequireArgCount(name, args, 1);
                        return Math.Sinh(args[0]);

                    case "sqrt":
                        RequireArgCount(name, args, 1);
                        return Math.Sqrt(args[0]);

                    case "tan":
                        RequireArgCount(name, args, 1);
                        return Math.Tan(args[0]);

                    case "tanh":
                        // provide the hyperbolic tangent value of the specified angle.
                        RequireArgCount(name, args, 1);
                        return Math.Tanh(args[0]);

                    default:
                        throw new ArgumentException($"Unknown function '{name}'");
                }
            }

            private static void RequireArgCount(string name, double[] args, int expected)
            {
                if (args.Length != expected)
                    throw new ArgumentException(
                        $"Function '{name}' expects {expected} argument(s) but got {args.Length}");
            }
        }
    }
}
#endif