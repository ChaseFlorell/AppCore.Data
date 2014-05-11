﻿using System;
using System.Linq.Expressions;
using FutureState.AppCore.Data.Tests.Helpers.Models;
using NUnit.Framework;

namespace FutureState.AppCore.Data.Tests.Unit
{
    [TestFixture]
    public class OrderByExpressionVisitorTests
    {
        public static OrderByExpressionVisitor OrderByExpression<TModel>(Expression<Func<TModel, object>> expression)
            where TModel : class, new()
        {
            var visitor = new OrderByExpressionVisitor().Visit(expression);
            return visitor;
        }

        [Test]
        public void ShouldBuildTheOrderExpressionFeildList()
        {
            // Setup
            const string expectedString = "[Email]";
            var actualExpression = OrderByExpression<StudentModel>(u => u.Email);

            // Execute
            var actualString = actualExpression.OrderByExpression;

            // Test
            Assert.AreEqual(expectedString, actualString);
        }

        [Test]
        public void ShouldBuildTheOrderExpressionFeildListForDateTime()
        {
            // Setup
            const string expectedString = "[CreatedDate]";
            var actualExpression = OrderByExpression<StudentModel>(u => u.CreatedDate);

            // Execute
            var actualString = actualExpression.OrderByExpression;

            // Test
            Assert.AreEqual(expectedString, actualString);
        }
    }
}