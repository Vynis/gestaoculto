using System.Threading.Tasks;
using GestaoCulto.Application.DTOs.Cultos;
using GestaoCulto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCulto.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cultos")]
    public class CultosController : ControllerBase
    {
        private readonly ICultoService _cultoService;

        public CultosController(ICultoService cultoService)
        {
            _cultoService = cultoService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            return Ok(await _cultoService.ListarAsync());
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Obter(long id)
        {
            var item = await _cultoService.ObterPorIdAsync(id);
            if (item == null)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            return Ok(item);
        }

        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CultoRequestDto dto)
        {
            var novo = await _cultoService.CriarAsync(dto);
            return CreatedAtAction(nameof(Obter), new { id = novo.Id }, novo);
        }

        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Atualizar(long id, [FromBody] CultoRequestDto dto)
        {
            var atualizado = await _cultoService.AtualizarAsync(id, dto);
            if (atualizado == null)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            return Ok(atualizado);
        }

        [Authorize(Roles = "ADMIN,GESTAO_CULTO")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Excluir(long id)
        {
            var ok = await _cultoService.ExcluirAsync(id);
            if (!ok)
            {
                return NotFound(new { mensagem = "Culto não encontrado." });
            }

            return Ok(new { mensagem = "Culto removido com sucesso." });
        }
    }
}
