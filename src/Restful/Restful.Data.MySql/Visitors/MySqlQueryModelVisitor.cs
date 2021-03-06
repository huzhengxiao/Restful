﻿using System;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Restful.Data.Common;
using Restful.Data.MySql.Common;
using Restful.Data.MySql.Linq;
using Restful.Data.MySql.SqlParts;

namespace Restful.Data.MySql.Visitors
{
    public class MySqlQueryModelVisitor : QueryModelVisitorBase
    {
        #region Member
        /// <summary>
        /// 查询聚合器
        /// </summary>
        private readonly MySqlQueryPartsAggregator queryPartsAggregator;
        
        /// <summary>
        /// 参数聚合器
        /// </summary>
        private readonly MySqlParameterAggregator parameterAggregator;
        #endregion

        #region MySqlQueryModelVisitor
        /// <summary>
        /// 构造方法
        /// </summary>
        public MySqlQueryModelVisitor()
        {
            this.queryPartsAggregator = new MySqlQueryPartsAggregator();
            this.parameterAggregator = new MySqlParameterAggregator();
        }
        #endregion

        #region VisitQueryModel
        /// <summary>
        /// 解析 QueryModel
        /// </summary>
        /// <param name="queryModel"></param>
        public override void VisitQueryModel( QueryModel queryModel )
        {
            queryModel.SelectClause.Accept( this, queryModel );
            queryModel.MainFromClause.Accept( this, queryModel );

            this.VisitBodyClauses( queryModel.BodyClauses, queryModel );
            this.VisitResultOperators( queryModel.ResultOperators, queryModel );
        }
        #endregion

        #region VisitResultOperator
        /// <summary>
        /// 解析返回操作表达式
        /// </summary>
        /// <param name="resultOperator"></param>
        /// <param name="queryModel"></param>
        /// <param name="index"></param>
        public override void VisitResultOperator( ResultOperatorBase resultOperator, QueryModel queryModel, int index )
        {
            if( resultOperator is SkipResultOperator )
            {
                SkipResultOperator @operator = resultOperator as SkipResultOperator;
                this.queryPartsAggregator.LimitParts.From = @operator.GetConstantCount();
            }
            else if( resultOperator is TakeResultOperator )
            {
                TakeResultOperator @operator = resultOperator as TakeResultOperator;
                this.queryPartsAggregator.LimitParts.Count = @operator.GetConstantCount();
            }
            else if( resultOperator is CountResultOperator || resultOperator is LongCountResultOperator )
            {
                this.queryPartsAggregator.SelectPart = "COUNT(*)";
            }
            else if( resultOperator is FirstResultOperator || resultOperator is SingleResultOperator )
            {
                this.queryPartsAggregator.LimitParts.From = 0;
                this.queryPartsAggregator.LimitParts.Count = 1;
            }
            else if( resultOperator is DistinctResultOperator )
            {
                this.queryPartsAggregator.IsDistinct = true;
            }
            else
            {
                if( resultOperator is AverageResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is MaxResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is MinResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is SumResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is ContainsResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is DefaultIfEmptyResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is ExceptResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is GroupResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is IntersectResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is OfTypeResultOperator )
                {
                    throw new NotSupportedException();
                }
                if( resultOperator is UnionResultOperator )
                {
                    throw new NotSupportedException();
                }
            }

            base.VisitResultOperator( resultOperator, queryModel, index );
        }
        #endregion

        #region VisitMainFromClause
        /// <summary>
        /// 解析 from 语句
        /// </summary>
        /// <param name="fromClause"></param>
        /// <param name="queryModel"></param>
        public override void VisitMainFromClause( MainFromClause fromClause, QueryModel queryModel )
        {
            string itemName = fromClause.ItemName == "<generated>_0" ? "T" : fromClause.ItemName.ToUpper();

            this.queryPartsAggregator.FromParts.Add( string.Format( "{0}{1}{0} {2}", Constants.Quote, fromClause.ItemType.Name, itemName ) );

            base.VisitMainFromClause( fromClause, queryModel );
        }
        #endregion

        #region VisitSelectClause
        /// <summary>
        /// 解析 select 语句
        /// </summary>
        /// <param name="selectClause"></param>
        /// <param name="queryModel"></param>
        public override void VisitSelectClause( SelectClause selectClause, QueryModel queryModel )
        {
            MySqlSelectClauseVisitor visitor = new MySqlSelectClauseVisitor( this.parameterAggregator );

            string selectParts = visitor.Translate( selectClause.Selector );

            queryPartsAggregator.SelectPart = selectParts;

            base.VisitSelectClause( selectClause, queryModel );
        }
        #endregion

        #region VisitWhereClause
        /// <summary>
        /// 解析 Where 语句
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="queryModel"></param>
        /// <param name="index"></param>
        public override void VisitWhereClause( WhereClause whereClause, QueryModel queryModel, int index )
        {
            MySqlWhereClauseVisitor visitor = new MySqlWhereClauseVisitor( this.parameterAggregator );

            string whereParts = visitor.Translate( whereClause.Predicate );

            queryPartsAggregator.WhereParts.Add( whereParts );

            base.VisitWhereClause( whereClause, queryModel, index );
        }
        #endregion

        #region VisitOrderByClause
        /// <summary>
        /// 解析 orderby 语句
        /// </summary>
        /// <param name="orderByClause"></param>
        /// <param name="queryModel"></param>
        /// <param name="index"></param>
        public override void VisitOrderByClause( OrderByClause orderByClause, QueryModel queryModel, int index )
        {
            foreach( var ordering in orderByClause.Orderings )
            {
                MySqlOrderByClauseVisitor visitor = new MySqlOrderByClauseVisitor( this.parameterAggregator );

                string orderByParts = visitor.Translate( ordering.Expression );

                string direction = ordering.OrderingDirection == OrderingDirection.Desc ? "DESC" : "ASC";

                queryPartsAggregator.OrderByParts.Add( string.Format( "{0} {1}", orderByParts, direction ) );
            }

            base.VisitOrderByClause( orderByClause, queryModel, index );
        }
        #endregion

        #region Translate
        /// <summary>
        /// 将 QueryModel 翻译成查询命令
        /// </summary>
        /// <param name="queryModel"></param>
        /// <returns></returns>
        internal SqlCmd Translate( QueryModel queryModel )
        {
            this.VisitQueryModel( queryModel );

            return new SqlCmd( queryPartsAggregator.ToString(), parameterAggregator.Parameters );
        }
        #endregion
    }
}
