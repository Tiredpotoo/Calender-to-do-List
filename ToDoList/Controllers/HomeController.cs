using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoList.Models;

namespace ToDoList.Controllers
{
    public class HomeController : Controller
    {
        private ToDoContext _context;
        private List<ToDoViewModel> _toDoModels = new List<ToDoViewModel>();
        public HomeController(ToDoContext ctx) => _context = ctx;

        public ViewResult Index(string id)
        {
            // load current filters and data needed for filter drop downs in ToDoViewModel
            var model = new ToDoViewModel { 
                Filters = new Filters(id),
                Categories = _context.Categories.ToList(),
                Statuses = _context.Statuses.ToList(),
                DueFilters = Filters.DueFilterValues
            };

            // get open tasks from database based on current filters
            IQueryable<ToDo> query = _context.ToDos
                .Include(t => t.Category).Include(t => t.Status);

            if (model.Filters.HasCategory) {
                query = query.Where(t => t.CategoryId == model.Filters.CategoryId);
            }
            if (model.Filters.HasStatus) {
                query = query.Where(t => t.StatusId == model.Filters.StatusId);
            }
            if (model.Filters.HasDue) {
                var today = DateTime.Today;
                if (model.Filters.IsPast)
                    query = query.Where(t => t.DueDate < today);
                else if (model.Filters.IsFuture)
                    query = query.Where(t => t.DueDate > today);
                else if (model.Filters.IsToday)
                    query = query.Where(t => t.DueDate == today);
            }
            model.Tasks = query.OrderBy(t => t.DueDate).ToList();

            return View(model);
        }

        [HttpGet]
        public ViewResult Add()
        {
            var model = new ToDoViewModel
            {
                Categories = _context.Categories.ToList(),
                Statuses = _context.Statuses.ToList(),
                CurrentTask = new ToDo { StatusId = "open" }  // set default value for drop-down
            };

            _toDoModels.Add(model);

            return View(model);
        }

        [HttpPost]
        public IActionResult Add(ToDoViewModel model)
        {
            if (ModelState.IsValid)
            {
                _context.ToDos.Add(model.CurrentTask);
                _context.SaveChanges();
                return RedirectToAction("Calendar");  // Redirect to Calendar to see all tasks
            }
            else
            {
                // If the model state is not valid, reload necessary data for the form
                model.Categories = _context.Categories.ToList();
                model.Statuses = _context.Statuses.ToList();
                return View(model);
            }
        }


        [HttpPost]
        public IActionResult Filter(string[] filter)
        {
            string id = string.Join('-', filter);
            return RedirectToAction("Index", new { ID = id });
        }

        [HttpPost]
        public IActionResult MarkComplete([FromRoute]string id, ToDo selected)
        {
            selected = _context.ToDos.Find(selected.Id)!;  // use null-forgiving operator to suppress null warning
            if (selected != null)
            {
                selected.StatusId = "closed";
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { ID = id });
        }

        [HttpPost]
        public IActionResult DeleteComplete(string id)
        {
            var toDelete = _context.ToDos
                .Where(t => t.StatusId == "closed").ToList();

            foreach(var task in toDelete)
            {
                _context.ToDos.Remove(task);
            }
            _context.SaveChanges();

            return RedirectToAction("Index", new { ID = id });
        }

        public IActionResult Calendar()
        {
            var models = new List<ToDoViewModel>();
            var allTasks = _context.ToDos.Include(t => t.Category).Include(t => t.Status).ToList();

            var model = new ToDoViewModel
            {
                Tasks = allTasks
            };

            models.Add(model);

            return View(models);  // Pass the list of models containing all tasks to the view
        }

    }
}