// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating UI forms
/// </summary>
public class FormBuilder
{
    private readonly Control _rootControl;

    /// <summary>
    /// FormBuilder constructor creates a form builder with vertical layout
    /// </summary>
    public FormBuilder()
    {
        _rootControl = new Control
        {
            ControlType = ControlTypes.Layout,
            Layout = LayoutTypes.VerticalLayout
        };
    }

    /// <summary>
    /// Sets the form layout type
    /// </summary>
    public FormBuilder WithLayout(LayoutTypes layout)
    {
        _rootControl.Layout = layout;
        return this;
    }

    /// <summary>
    /// Sets the form title
    /// </summary>
    public FormBuilder WithTitle(string title)
    {
        _rootControl.Title = title;
        return this;
    }

    /// <summary>
    /// Adds a control to the form
    /// </summary>
    public FormBuilder AddControl(Action<ControlBuilder> configure)
    {
        var builder = new ControlBuilder();
        configure(builder);
        _rootControl.Elements.Add(builder.Build());
        return this;
    }

    internal Control Build() => _rootControl;
}

/// <summary>
/// Fluent builder for creating form controls
/// </summary>
public class ControlBuilder
{
    private readonly Control _control;

    /// <summary>
    /// ControlBuilder constructor creates a control builder for UI elements
    /// </summary>
    public ControlBuilder()
    {
        _control = new Control();
    }

    /// <summary>
    /// Sets the control type
    /// </summary>
    public ControlBuilder OfType(ControlTypes type)
    {
        _control.ControlType = type;
        return this;
    }

    /// <summary>
    /// Sets the control title/label
    /// </summary>
    public ControlBuilder WithTitle(string title)
    {
        _control.Title = title;
        return this;
    }

    /// <summary>
    /// Binds the control to a data field (JSON Pointer scope)
    /// </summary>
    public ControlBuilder BoundTo(string scope)
    {
        _control.Scope = scope;
        return this;
    }

    /// <summary>
    /// Sets the layout type (for container controls)
    /// </summary>
    public ControlBuilder WithLayout(LayoutTypes layout)
    {
        _control.Layout = layout;
        return this;
    }

    /// <summary>
    /// Adds a child control (for container controls)
    /// </summary>
    public ControlBuilder AddChild(Action<ControlBuilder> configure)
    {
        var builder = new ControlBuilder();
        configure(builder);
        _control.Elements.Add(builder.Build());
        return this;
    }

    internal Control Build() => _control;
}
