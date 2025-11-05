using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation;

/// <summary>
/// Registry for managing custom validation rules.
/// </summary>
public class CustomValidationRuleRegistry
{
    private readonly ConcurrentDictionary<string, Func<object, CancellationToken, ValueTask<IEnumerable<string>>>> _rules = new();

    /// <summary>
    /// Registers a custom validation rule.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <param name="validationFunc">The validation function.</param>
    public void RegisterRule(string name, Func<object, CancellationToken, ValueTask<IEnumerable<string>>> validationFunc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Rule name cannot be null or empty", nameof(name));
        }

        _rules[name] = validationFunc ?? throw new ArgumentNullException(nameof(validationFunc));
    }

    /// <summary>
    /// Gets a registered validation rule by name.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <returns>The validation function if found, null otherwise.</returns>
    public Func<object, CancellationToken, ValueTask<IEnumerable<string>>>? GetRule(string name)
    {
        return _rules.TryGetValue(name, out var rule) ? rule : null;
    }

    /// <summary>
    /// Checks if a rule is registered.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <returns>True if the rule is registered, false otherwise.</returns>
    public bool IsRuleRegistered(string name)
    {
        return _rules.ContainsKey(name);
    }

    /// <summary>
    /// Removes a registered rule.
    /// </summary>
    /// <param name="name">The name of the rule to remove.</param>
    /// <returns>True if the rule was removed, false if it wasn't found.</returns>
    public bool RemoveRule(string name)
    {
        return _rules.TryRemove(name, out _);
    }

    /// <summary>
    /// Gets all registered rule names.
    /// </summary>
    /// <returns>A collection of registered rule names.</returns>
    public IEnumerable<string> GetRegisteredRuleNames()
    {
        return _rules.Keys;
    }

    /// <summary>
    /// Clears all registered rules.
    /// </summary>
    public void Clear()
    {
        _rules.Clear();
    }
}