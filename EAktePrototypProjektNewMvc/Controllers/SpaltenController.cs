using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models;
using EAktePrototypProjektNewMvc.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EAktePrototypProjektNewMvc.Controllers
{
    [Route("spalten")]
    public class SpaltenController : Controller
    {
        private readonly TableColumnRepository _columnRepository;
        private readonly ILogger<SpaltenController> _logger;

        public SpaltenController(TableColumnRepository columnRepository, ILogger<SpaltenController> logger)
        {
            _columnRepository = columnRepository;
            _logger = logger;
        }

        // User ID aus der Session abrufen(UId).
        private string Uid => HttpContext.Session.GetString("UId");

        // GET: /spalten/{tableKey} Gibt die Spalteneinstellungen für die angegebene Tabelle zurück. 
        [HttpGet("{tableKey}")]
        public async Task<IActionResult> Get(string tableKey)
        {
            try
            {
                var columns = await _columnRepository.GetColumnsForEditAsync(tableKey, Uid);
                if (columns.Count == 0) return NotFound();
                return Json(columns);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Spalten für {TableKey}", tableKey);
                return StatusCode(500);
            }
        }
        
        // POST: /spalten/{tableKey} Speichert die Spalteneinstellungen für die angegebene Tabelle.
        [HttpPost("{tableKey}")]
        public async Task<IActionResult> Save(string tableKey, [FromBody] List<UserColumnSetting> settings)
        {
            try
            {
                if (settings == null) return BadRequest();
                var ok = await _columnRepository.SaveUserColumnsAsync(tableKey, Uid, settings);
                return ok ? Ok() : BadRequest("Ungültige Spaltenauswahl.");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Spalten für {TableKey}", tableKey);
                return StatusCode(500);
            }
        }

        // DELETE: /spalten/{tableKey} Setzt die Spalteneinstellungen für die angegebene Tabelle auf die Standardwerte zurück.
        [HttpDelete("{tableKey}")]
        public async Task<IActionResult> Reset(string tableKey)
        {
            try
            {
                await _columnRepository.ResetUserColumnsAsync(tableKey, Uid);
                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Zurücksetzen der Spalten für {TableKey}", tableKey);
                return StatusCode(500);
            }
        }


        // DELETE /spalten/alle Setzt die Spalteneinstellungen für ALLE Tabellen zurück.
        [HttpDelete("alle")]
        public async Task<IActionResult> ResetAlle()
        {
            try
            {
                await _columnRepository.ResetAllUserColumnsAsync(Uid);
                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Zurücksetzen aller Spalten");
                return StatusCode(500);
            }
        }



    }
}