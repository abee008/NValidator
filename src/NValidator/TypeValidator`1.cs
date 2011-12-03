﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NValidator.Builders;

namespace NValidator
{
    /// <summary>
    /// A base class for validating a type without performing deep validation on its properties even though the properties have validators registered
    /// <para>For example, Order has a list of OrderDetails. There are OrderValidator and OrderDetailValidator registered.</para>
    /// <para>If we perform validation on Order using OrderValidator, it will not perform validation on those OrderDetails using their OrderDetailValidators</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TypeValidator<T> : BaseValidator<T>, IHaveContainer
    {
        public string ContainerName { get; protected set; }
        /// <summary>
        /// Gets or sets the default type of the validation builder.
        /// The default type can be set via ValidatorFactory.DefaultValidationBuilderType, default value is typeof(ValidationBuilder&lt;,&gt;) or set in the constructor
        /// </summary>
        /// <value>
        /// The default type of the builder.
        /// </value>
        public Type DefaultBuilderType { get; set; }
        protected List<IValidationBuilder<T>> ValidationBuilders { get; set; }

        protected TypeValidator()
        {
            ValidationBuilders = new List<IValidationBuilder<T>>();
            DefaultBuilderType = ValidatorFactory.DefaultValidationBuilderType;
        }

        public void UpdateContainerName(string containerName)
        {
            ContainerName = containerName;
            foreach (var v in ValidationBuilders)
            {
                if (v != null)
                {
                    v.UpdateContainerName(containerName);
                }
            }
        }

        public IFluentValidationBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var newBuilder = CreateGenericBuilder(expression);
            newBuilder.UpdateContainerName(ContainerName);
            ValidationBuilders.Add(newBuilder);
            return newBuilder;
        }

        public sealed override IEnumerable<ValidationResult> GetValidationResult(T value, ValidationContext validationContext)
        {
            return InternalGetValidationResult(value, validationContext);
        }

        internal virtual protected IEnumerable<ValidationResult> InternalGetValidationResult(T value, ValidationContext validationContext)
        {
            validationContext = validationContext ?? new ValidationContext {ContainerInstance = value};
            foreach (var b in ValidationBuilders)
            {
                var builder = b;
                while (builder != null)
                {
                    if (validationContext.ShouldIgnore(ContainerName))
                    {
                        continue;// while, go to next builder
                    }

                    if (ContainerName != null)
                    {
                        builder.UpdateContainerName(ContainerName);
                    }

                    var results = builder.Validate(value, validationContext).ToList();
                    var foundError = false;
                    foreach (var validationResult in results)
                    {
                        if (validationContext.ShouldIgnore(validationResult.MemberName))
                        {
                            continue;
                        }
                        foundError = true;
                        yield return validationResult;
                    }

                    if (foundError && builder.StopChainOnError)
                    {
                        break; // while, dont go to next builder
                    }

                    builder = builder.Next;
                }
            }
        }

        protected virtual IValidationBuilder<T, TProperty> CreateGenericBuilder<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return ValidationBuilderHelpers.CreateGenericBuilder(expression, DefaultBuilderType);
        }
    }
}
