using LeaveManagementSystem.Web.Services.LeaveAllocations;
using LeaveManagementSystem.Web.Services.LeaveTypes;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.Web.Controllers
{
    [Authorize]
    public class LeaveAllocationController(ILeaveAllocationsService _leaveAllocationsService, ILeaveTypesService _leaveTypesService) : Controller
    {

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Index()
        {

            var employees = await _leaveAllocationsService.GetEmployees();
            return View(employees);
        }


        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateLeave(string? id)
        {
             await _leaveAllocationsService.AllocateLeave(id);
            return RedirectToAction(nameof(Details), new {userId = id});
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditAllocation(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var allocation = await _leaveAllocationsService.GetEmployeeAllocation(id.Value);
            if (allocation == null)
            {
                return NotFound();
            }
            return View(allocation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAllocation(LeaveAllocationEditVM allocation)
        {
            if(await _leaveTypesService.DaysExceedMaximum(allocation.LeaveType.Id, allocation.Days))
            {
                 ModelState.AddModelError("Days", $"Maximum Leave days exceeded!");
            }
            if (ModelState.IsValid)
            {
                await _leaveAllocationsService.EditAllocation(allocation);
                return RedirectToAction(nameof(Details), new { userId = allocation.Employee.Id });
            }
            //tracking the days value as it gets reset when returning to the view if the model state is invalid
            var days = allocation.Days;
            allocation = await _leaveAllocationsService.GetEmployeeAllocation(allocation.Id); //refetch the allocation from the database
            allocation.Days = days; //set the days to the value entered by the user
            return View(allocation);
        }

        public async Task<IActionResult> Details(string? userId)
        {
          
          var employeeVm = await  _leaveAllocationsService.GetEmployeeAllocations(userId);
          return View(employeeVm);
        }
    }
}
