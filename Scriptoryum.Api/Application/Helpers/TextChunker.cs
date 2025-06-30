using System;
using System.Collections.Generic;

namespace Scriptoryum.Api.Application.Helpers;

public static class TextChunker
{
    /// <summary>
    /// Divide o texto em chunks de tamanho máximo (em caracteres), preferencialmente em quebras de parágrafo.
    /// </summary>
    public static List<string> ChunkText(string text, int maxChunkSize = 12000)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);
        var currentChunk = "";

        foreach (var para in paragraphs)
        {
            // +2 para considerar a quebra de parágrafo
            if ((currentChunk.Length + para.Length + 2) > maxChunkSize)
            {
                if (!string.IsNullOrWhiteSpace(currentChunk))
                    chunks.Add(currentChunk.Trim());
                currentChunk = para + "\r\n\r\n";
            }
            else
            {
                currentChunk += para + "\r\n\r\n";
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
            chunks.Add(currentChunk.Trim());

        return chunks;
    }
}