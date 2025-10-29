using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.ContractValidation.CustomValidators;

/// <summary>
/// Default implementation of <see cref="IValidatorRegistry"/> for registering and discovering custom validators.
/// </summary>
public sealed class ValidatorRegistry : IValidatorRegistry
{
    private readonly List<ICustomValidator> _validators = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Register(ICustomValidator validator)
    {
        if (validator == null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        lock (_lock)
        {
            if (!_validators.Contains(validator))
            {
                _validators.Add(validator);
            }
        }
    }

    /// <inheritdoc />
    public void Register<TValidator>() where TValidator : ICustomValidator, new()
    {
        var validator = new TValidator();
        Register(validator);
    }

    /// <inheritdoc />
    public bool Unregister(ICustomValidator validator)
    {
        if (validator == null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        lock (_lock)
        {
            return _validators.Remove(validator);
        }
    }

    /// <inheritdoc />
    public int UnregisterAll<TValidator>() where TValidator : ICustomValidator
    {
        lock (_lock)
        {
            var toRemove = _validators.Where(v => v is TValidator).ToList();
            foreach (var validator in toRemove)
            {
                _validators.Remove(validator);
            }
            return toRemove.Count;
        }
    }

    /// <inheritdoc />
    public IEnumerable<ICustomValidator> GetAll()
    {
        lock (_lock)
        {
            return _validators.ToList();
        }
    }

    /// <inheritdoc />
    public IEnumerable<ICustomValidator> GetValidatorsFor(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        lock (_lock)
        {
            return _validators.Where(v => v.AppliesTo(type)).ToList();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _validators.Clear();
        }
    }
}
