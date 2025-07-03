// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Common.Json;

public class TrueTypeJsonAttribute<T>() : JsonConverterAttribute(typeof(TrueTypeJsonConverter<T>)) where T : notnull
{

}