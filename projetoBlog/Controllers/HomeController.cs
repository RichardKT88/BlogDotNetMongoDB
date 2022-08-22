using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using projetoBlog.Models;
using projetoBlog.Models.Home;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Bson;

namespace projetoBlog.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var conectandoMongoDB = new AcessoMongoDB();

            var filtro = new BsonDocument();
            var publicacoesRecentes = await conectandoMongoDB.Publicacoes.Find(filtro).SortByDescending(x => x.DataCriacao).Limit(10).ToListAsync();

            var model = new IndexModel
            {
                PublicacoesRecentes = publicacoesRecentes
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult NovaPublicacao()
        {
            return View(new NovaPublicacaoModel());
        }

        [HttpPost]
        public async Task<ActionResult> NovaPublicacao(NovaPublicacaoModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var post = new Publicacao
            {
                Autor = User.Identity.Name,
                Titulo = model.Titulo,
                Conteudo = model.Conteudo,
                Tags = model.Tags.Split(' ',',', ';'),
                DataCriacao = DateTime.UtcNow,
                Comentarios = new List<Comentario>()
            };

            var conectandoMongoDB = new AcessoMongoDB();
            await conectandoMongoDB.Publicacoes.InsertOneAsync(post);

            return RedirectToAction("Publicacao", new { id = post.Id });
        }

        [HttpGet]
        public async Task<ActionResult> Publicacao(string id)
        {

            var connectarMongoDB = new AcessoMongoDB();
            var publicacao = await connectarMongoDB.Publicacoes.Find(x => x.Id == id).SingleOrDefaultAsync();

            if (publicacao == null)
            {
                return RedirectToAction("Index");
            }

            var model = new PublicacaoModel
            {
                Publicacao = publicacao,
                NovoComentario = new NovoComentarioModel
                {
                    PublicacaoId = id
                }
            };

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Publicacoes(string tag = null)
        {
            var connectarMongoDB = new AcessoMongoDB();
            var posts = new List<Publicacao>();
            if(tag != null)
            {
                var filtro = new BsonDocument();
                posts = await connectarMongoDB.Publicacoes.Find(filtro).SortByDescending(x => x.DataCriacao).Limit(10).ToListAsync();
            }
            else 
            {
                var construtor = Builders<Publicacao>.Filter;
                var condicao = construtor.AnyEq(x => x.Tags, tag);
                posts = await connectarMongoDB.Publicacoes.Find(condicao).SortByDescending(x => x.DataCriacao).Limit(10).ToListAsync();
            }          

            return View(posts);
        }

        [HttpPost]
        public async Task<ActionResult> NovoComentario(NovoComentarioModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Publicacao", new { id = model.PublicacaoId });
            }

            var comment = new Comentario
            {
                Autor = User.Identity.Name,
                Conteudo = model.Conteudo,
                DataCriacao = DateTime.UtcNow
            };

            var conectandoMongoDB = new AcessoMongoDB();

            var construtor = Builders<Publicacao>.Filter;
            var condicao = construtor.Eq(x => x.Id, model.PublicacaoId);

            var construtorAlteracao = Builders<Publicacao>.Update;
            var condicaoAlteracao = construtorAlteracao.Push(x => x.Comentarios, comment);
            await conectandoMongoDB.Publicacoes.UpdateOneAsync(condicao, condicaoAlteracao);

            return RedirectToAction("Publicacao", new { id = model.PublicacaoId });
        }
    }
}