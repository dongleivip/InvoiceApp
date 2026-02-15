// <copyright file="ApiResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace InvoiceApi.DTO;

/// <summary>
///     Generic API response wrapper for consistent response structure
/// </summary>
/// <typeparam name="T">Type of data payload</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    ///     Gets a value indicating whether indicates if the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets the data payload (null for void operations or failures).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    ///     Gets optional message for additional context.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Gets error code for failed operations.
    /// </summary>
    public string? ErrorCode { get; init; }
}