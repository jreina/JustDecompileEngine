﻿#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion
using System;
using System.Collections;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Pattern;

namespace Telerik.JustDecompiler.Steps
{
	class VariableComparer : IEqualityComparer
	{
		public bool Equals(object x, object y)
		{
			if (x == y)
				return true;

			if (x == null)
				return y == null;

            var x_arg_ref = x as ArgumentReferenceExpression;
            var y_arg_ref = y as ArgumentReferenceExpression;

			if (x_arg_ref != null && y_arg_ref != null)
				return x_arg_ref.Parameter == y_arg_ref.Parameter;

            var x_var_ref = x as VariableReferenceExpression;
            var y_var_ref = y as VariableReferenceExpression;

			if (x_var_ref != null && y_var_ref != null)
				return x_var_ref.Variable == y_var_ref.Variable;

			return false;
		}

		public int GetHashCode(object obj)
		{
			return obj.GetHashCode();
		}
	}

	public class SelfAssignement : BaseCodeTransformer, IDecompilationStep
	{
		const string TargetKey = "Target";
		const string OperatorKey = "Operator";

		static readonly Pattern.ICodePattern SelfAssignmentPattern = new Pattern.Assignment
		{
			Bind = assign => new Pattern.MatchData(TargetKey, assign.Left),
			Expression = new Pattern.Binary
			{
				Bind = binary => new Pattern.MatchData(OperatorKey, binary.Operator),
				Left = new Pattern.ContextData { Name = TargetKey, Comparer = new VariableComparer() },
				Right = new Pattern.Literal { Value = 1 }
			}
		};

        public override ICodeNode VisitBinaryExpression(BinaryExpression node)
        {
            if (node.IsAssignmentExpression)
            {
                return VisitAssignExpression(node);
            }
            return base.VisitBinaryExpression(node);
        }

		private ICodeNode VisitAssignExpression(BinaryExpression node)
		{
			MatchContext result = Pattern.CodePattern.Match(SelfAssignmentPattern, node);
			if (!result.Success)
			{
				return base.VisitBinaryExpression(node);
			}

			Expression target = (Expression)result[TargetKey];
			BinaryOperator @operator = (BinaryOperator)result[OperatorKey];

			switch (@operator)
			{
				case BinaryOperator.Add:
				case BinaryOperator.Subtract:
					return new UnaryExpression(
						GetCorrespondingOperator(@operator), target.CloneExpressionOnly(), node.UnderlyingSameMethodInstructions);
				default:
                    return base.VisitBinaryExpression(node);
			}
		}

		static UnaryOperator GetCorrespondingOperator(BinaryOperator @operator)
		{
			switch (@operator)
			{
				case BinaryOperator.Add:
					return UnaryOperator.PostIncrement;
				case BinaryOperator.Subtract:
					return UnaryOperator.PostDecrement;
				default:
					throw new ArgumentException();
			}
		}

		public BlockStatement Process(DecompilationContext context, BlockStatement body)
		{
			return (BlockStatement) VisitBlockStatement(body);
		}
	}
}