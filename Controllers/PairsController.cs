using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MiniApp.Core.Repositories;
using MiniApp.Dtos;

namespace MiniApp.Controllers;

[ApiController]
[Route("api/pairs")]
public class PairsController : ControllerBase
{
    private readonly IPairRepository _pairRepository;

    public PairsController(IPairRepository pairRepository)
    {
        _pairRepository = pairRepository;
    }

    [HttpGet]
    public async Task<ObjectResult> GetPairs(int? code, string? value)
    {
        return Ok(await _pairRepository.GetAllAsync(code, value));
    }

    [HttpPost]
    public async Task<ObjectResult> CreatePairs()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        
        var data = await reader.ReadToEndAsync();

        var rawPairs = JsonSerializer.Deserialize<Dictionary<int, string>[]>(data);

        if (rawPairs is null)
        {
            return BadRequest("Empty body");
        }
        
        var createPairDtos = rawPairs
            .Select(rp =>
            {
                var code = rp.Keys.First();
                return new CreatePairDto { Code = code, Value = rp[code] };
            })
            .OrderBy(p => p.Code);

        await _pairRepository.ClearAll();

        await _pairRepository.CreateManyAsync(createPairDtos);

        return CreatedAtAction(nameof(GetPairs), null);
    }

    private void SelectClients()
    {
        const string queries = @"
        -- TASK 2
        CREATE TABLE Clients (
	        Id BIGINT PRIMARY KEY IDENTITY(1, 1),
	        Name NVARCHAR(200)
        )

        CREATE TABLE ClientContacts (
	        Id BIGINT PRIMARY KEY IDENTITY(1, 1),
	        Type NVARCHAR(255) NULL,
	        Value NVARCHAR(255) NULL,
	        ClientId BIGINT NOT NULL FOREIGN KEY REFERENCES Clients(Id)
        )

        -- Select every client with the number of contacts
        SELECT [c].[Id], [c].[Name], COUNT([cc].[Id]) AS [NumberOfContacts] FROM [Clients] AS [c]
        	LEFT OUTER JOIN [ClientContacts] AS [cc] ON [c].[Id] = [cc].[ClientId]
        	GROUP BY [c].[Id], [c].[Name]
        	ORDER BY [c].[Id];

        -- Select client who has more than 2 contacts
        SELECT [c].[Id], [c].[Name], COUNT([cc].[Id]) AS [NumberOfContacts] FROM [Clients] AS [c]
        	LEFT OUTER JOIN [ClientContacts] AS [cc] ON [c].[Id] = [cc].[ClientId]
        	GROUP BY [c].[Id], [c].[Name]
        	HAVING COUNT([cc].[Id]) > 2
        	ORDER BY [c].[Id];


        -- TASK 3
        CREATE TABLE Dates (
            Id BIGINT NOT NULL,
            Date DATETIME2 NOT NULL
        )
        
        SELECT [left].[Id] AS [leftId], [left].[Date] AS [leftDate], [right].[Date] AS [rightDate]
		INTO [DateIntervals]
		FROM 
			(
			SELECT [Id], [Date] 
			FROM [Dates]
			) AS [left]

			INNER JOIN

			(
			SELECT [Id], [Date]
			FROM [Dates]
			) AS [right] ON [left].[Id] = [right].[Id]

		WHERE [left].[Date] < [right].[Date]
		ORDER BY [leftId], [leftDate], [rightDate]

		; WITH [TableDateIntervalsWithRowID] AS
		(
			SELECT ROW_NUMBER() OVER (PARTITION BY [leftDate] ORDER BY [leftDate]) AS [RowID], [leftId], [leftDate], [rightDate]
			FROM [DateIntervals]
		)


		DELETE FROM [TableDateIntervalsWithRowID] WHERE [RowID] > 1


		SELECT * FROM [DateIntervals]
		

		DROP TABLE [DateIntervals]
        ";
    }
}