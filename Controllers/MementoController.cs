using Memento.Data;
using Memento_Grupo1.Memento;
using Memento_Grupo1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Memento.Controllers
{
    public class MementoController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly Caretaker caretaker = new Caretaker();
        private readonly Originator originator = new Originator();
        private int savepoint = 0;

        public MementoController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            var blogPosts = _context.BlogPosts.ToList(); // Obtiene todos los artículos de blog
            return View(blogPosts); // Pasa la lista a la vista
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Retorna la vista para crear un nuevo artículo de blog
            return View();
        }

        [HttpPost]
        public IActionResult Create(BlogPost blogPost)
        {
            if (ModelState.IsValid)
            {
                _context.BlogPosts.Add(blogPost);
                _context.SaveChanges();

                // Guarda el estado actual del artículo en el Memento
                originator.State = JsonConvert.SerializeObject(blogPost);
                caretaker.Add(originator.SaveStateToMemento());
                savepoint++;

                return RedirectToAction("Index");
            }
            return View(blogPost);
        }

        public IActionResult Undo()
        {
            if (savepoint > 0)
            {
                savepoint--;
                var memento = caretaker.Get(savepoint);
                originator.GetStateFromMemento(memento);
                var blogPost = JsonConvert.DeserializeObject<BlogPost>(originator.State);

                UpdateBlogPostInDb(blogPost); // Asegúrate de que este método funciona correctamente

                return RedirectToAction("Index");
            }

            // Si no hay estado para deshacer, simplemente redirige a Index
            return RedirectToAction("Index");
        }

        private void UpdateBlogPostInDb(BlogPost blogPost)
        {
            // Actualiza el artículo en la base de datos
            var existingPost = _context.BlogPosts.Find(blogPost.Id);
            if (existingPost != null)
            {
                existingPost.Title = blogPost.Title;
                existingPost.Content = blogPost.Content;
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var blogPost = _context.BlogPosts.Find(id);
            if (blogPost == null)
            {
                return NotFound();
            }

            return View(blogPost);
        }

        [HttpPost]
        public IActionResult Edit(BlogPost blogPost)
        {
            if (ModelState.IsValid)
            {
                var currentPost = _context.BlogPosts.AsNoTracking().FirstOrDefault(p => p.Id == blogPost.Id);
                if (currentPost != null)
                {
                    originator.State = JsonConvert.SerializeObject(currentPost);
                    caretaker.Add(originator.SaveStateToMemento());
                    savepoint++; // Asegúrate de que esta línea se ejecuta correctamente
                }

                _context.BlogPosts.Update(blogPost);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(blogPost);
        }

    }
}
