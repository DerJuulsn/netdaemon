﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NetDaemon.HassModel.CodeGenerator.Model;

internal record Selector()
{
    public string? Type { get; init; }
}


internal record AreaSelector : Selector
{
    public DeviceSelector? Device { get; init; }

    public EntitySelector? Entity { get; init; }
}

internal record DeviceSelector : Selector
{
    public string? Integration { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public EntitySelector? Entity { get; init; }
}

internal record EntitySelector : Selector
{
    public string? Integration { get; init; }

    [JsonConverter(typeof(StringAsArrayConverter))]
    public string[] Domain { get; init; } = Array.Empty<string>();
}

internal record NumberSelector : Selector
{
    [Required]
    public double Min { get; init; }

    [Required]
    public double Max { get; init; }

    public float? Step { get; init; }

    public string? UnitOfMeasurement { get; init; }
}

internal record TargetSelector : Selector 
{
    public AreaSelector? Area { get; init; }

    public DeviceSelector? Device { get; init; }

    public EntitySelector? Entity { get; init; }
}

