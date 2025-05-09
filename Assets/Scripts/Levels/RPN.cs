using System;
using System.Collections.Generic;
using System.Globalization; // Required for CultureInfo
using UnityEngine; // For Mathf.FloorToInt

public static class RPNEvaluator // Renamed from RPN
{
    public static int EvaluateInt(string expression, Dictionary<string, float> variables)
    {
        if (string.IsNullOrEmpty(expression))
        {
            // Attempt to get a default value if specified, otherwise throw or return a sensible default.
            // For now, let's assume expressions are expected to be valid or handled by caller.
            // Consider if a "base" or default value should be passed in variables for empty expressions.
            if (variables.TryGetValue("base", out float baseVal))
            {
                return Mathf.FloorToInt(baseVal);
            }
            Debug.LogWarning($"RPN.EvaluateInt: Empty expression and no 'base' variable. Returning 0.");
            return 0; 
        }

        Stack<int> stack = new Stack<int>();
        string[] tokens = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (int.TryParse(token, out int number))
            {
                stack.Push(number);
            }
            else if (variables.ContainsKey(token.ToLowerInvariant()))
            {
                stack.Push(Mathf.FloorToInt(variables[token.ToLowerInvariant()]));
            }
            else
            {
                if (stack.Count < 2) throw new ArgumentException($"Invalid RPN expression: '{expression}' - Operator '{token}' needs two operands, stack size is {stack.Count}.");
                int operand2 = stack.Pop();
                int operand1 = stack.Pop();

                switch (token)
                {
                    case "+":
                        stack.Push(operand1 + operand2);
                        break;
                    case "-":
                        stack.Push(operand1 - operand2);
                        break;
                    case "*":
                        stack.Push(operand1 * operand2);
                        break;
                    case "/":
                        if (operand2 == 0) throw new DivideByZeroException($"Invalid RPN expression: '{expression}' - Division by zero.");
                        stack.Push(Mathf.FloorToInt((float)operand1 / operand2)); // Integer division
                        break;
                    case "%":
                         if (operand2 == 0) throw new DivideByZeroException($"Invalid RPN expression: '{expression}' - Modulo by zero.");
                         stack.Push(operand1 % operand2);
                         break;
                    default:
                        throw new ArgumentException($"Invalid RPN expression: '{expression}' - Unknown token '{token}'.");
                }
            }
        }

        if (stack.Count != 1) throw new ArgumentException($"Invalid RPN expression: '{expression}' - Expression did not resolve to a single value. Stack count: {stack.Count}.");
        return stack.Peek();
    }

    public static float EvaluateFloat(string expression, Dictionary<string, float> variables)
    {
        if (string.IsNullOrEmpty(expression))
        {
            if (variables.TryGetValue("base", out float baseVal))
            {
                return baseVal;
            }
            Debug.LogWarning($"RPN.EvaluateFloat: Empty expression and no 'base' variable. Returning 0f.");
            return 0f;
        }

        Stack<float> stack = new Stack<float>();
        string[] tokens = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (float.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out float number))
            {
                stack.Push(number);
            }
            else if (variables.ContainsKey(token.ToLowerInvariant()))
            {
                stack.Push(variables[token.ToLowerInvariant()]);
            }
            else
            {
                if (stack.Count < 2) throw new ArgumentException($"Invalid RPN expression: '{expression}' - Operator '{token}' needs two operands, stack size is {stack.Count}.");
                float operand2 = stack.Pop();
                float operand1 = stack.Pop();

                switch (token)
                {
                    case "+":
                        stack.Push(operand1 + operand2);
                        break;
                    case "-":
                        stack.Push(operand1 - operand2);
                        break;
                    case "*":
                        stack.Push(operand1 * operand2);
                        break;
                    case "/":
                        if (operand2 == 0f) throw new DivideByZeroException($"Invalid RPN expression: '{expression}' - Division by zero.");
                        stack.Push(operand1 / operand2);
                        break;
                    case "%":
                         if (operand2 == 0f) throw new DivideByZeroException($"Invalid RPN expression: '{expression}' - Modulo by zero.");
                         stack.Push(operand1 % operand2);
                         break;
                    default:
                        throw new ArgumentException($"Invalid RPN expression: '{expression}' - Unknown token '{token}'.");
                }
            }
        }

        if (stack.Count != 1) throw new ArgumentException($"Invalid RPN expression: '{expression}' - Expression did not resolve to a single value. Stack count: {stack.Count}.");
        return stack.Peek();
    }

    public static string EvaluateString(string expression, Dictionary<string, float> variables)
    {
        // For things like "type": "arcane"
        // String expressions are not typically evaluated with RPN in this context,
        // but variables could potentially be substituted if needed in the future.
        // For now, just return the expression as is.
        return expression;
    }
}
