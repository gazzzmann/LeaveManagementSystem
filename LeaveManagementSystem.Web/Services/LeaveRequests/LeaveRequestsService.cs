using LeaveManagementSystem.Web.Models.LeaveRequests;
using LeaveManagementSystem.Web.Services.LeaveAllocations;
using LeaveManagementSystem.Web.Services.LeaveRequests;
using LeaveManagementSystem.Web.Services.Users;

namespace LeaveManagementSystem.Web.Services.LeaveRequests
{
    public class LeaveRequestsService(IMapper _mapper, IUserService _userService, ApplicationDbContext _context, ILeaveAllocationsService _leaveAllocationsService ) : ILeaveRequestsService
    {
        public async Task CancelLeaveRequest(int leaveRequestId)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(leaveRequestId);
            leaveRequest.LeaveRequestStatusId = (int)LeaveRequestStatusEnum.Canceled;

            //restore allocation days based on requested days
            await UpdateAllocationDays(leaveRequest, false);
            await _context.SaveChangesAsync();
            
        }

        public async Task CreateLeaveRequest(LeaveRequestCreateVM model)
        {
            //map data to leave request data model
            var leaveRequest = _mapper.Map<LeaveRequest>(model);

            //get logged in employee id
            var user = await _userService.GetLoggedInUser(); 
            leaveRequest.EmployeeId = user.Id;

            //set LeaveRequestStatusId to pending
            leaveRequest.LeaveRequestStatusId = (int)LeaveRequestStatusEnum.Pending;

            //save leave request to db
            _context.LeaveRequests.Add(leaveRequest);


            //deduct allocation days based on requested days
            await UpdateAllocationDays(leaveRequest, true); 
            await _context.SaveChangesAsync();
        }

        public async Task<EmployeeLeaveRequestListVM> AdminGetAllLeaveRequests()
        {
            var leaveRequests = await _context.LeaveRequests
                 .Include(q => q.LeaveType)
                 .ToListAsync();

            var numberOfApprovedLeaveRequests = leaveRequests
                .Count(q => q.LeaveRequestStatusId == (int)LeaveRequestStatusEnum.Approved);
            var numberOfPendingLeaveRequests = leaveRequests
              .Count(q => q.LeaveRequestStatusId == (int)LeaveRequestStatusEnum.Pending);
            var numberOfDeclinedLeaveRequests = leaveRequests
              .Count(q => q.LeaveRequestStatusId == (int)LeaveRequestStatusEnum.Declined);

            var leaveRequestModels = leaveRequests.Select(q => new LeaveRequestReadOnlyVM
            {
                Id = q.Id,
                StartDate = q.StartDate,
                EndDate = q.EndDate,
                LeaveType = q.LeaveType.Name,
                NumberOfDays = q.EndDate.DayNumber - q.StartDate.DayNumber,
                LeaveRequestStatus = (LeaveRequestStatusEnum)q.LeaveRequestStatusId
            }).ToList();

            var model = new EmployeeLeaveRequestListVM
            {
                ApprovedRequests = numberOfApprovedLeaveRequests,
                PendingRequests = numberOfPendingLeaveRequests,
                DeclinedRequests = numberOfDeclinedLeaveRequests,
                TotalRequests = leaveRequests.Count,
                LeaveRequests = leaveRequestModels
            };

            return model;
        }
        public async Task<List<LeaveRequestReadOnlyVM>> GetEmployeeLeaveRequests()
        {
           var user = await _userService.GetLoggedInUser();
            var leaveRequests = await _context.LeaveRequests
                 .Include(q => q.LeaveType)
                 .Where(q => q.EmployeeId == user.Id)
                 .ToListAsync();

            var model = leaveRequests.Select(q => new LeaveRequestReadOnlyVM
            {
                Id = q.Id,
                StartDate = q.StartDate,
                EndDate = q.EndDate,
                LeaveType = q.LeaveType.Name,
                NumberOfDays = q.EndDate.DayNumber - q.StartDate.DayNumber,
                LeaveRequestStatus = (LeaveRequestStatusEnum)q.LeaveRequestStatusId
            }).ToList();

            return model;
        }

        public async Task<bool> RequestDatesExceedAllocation(LeaveRequestCreateVM model)
        {
            var user = await _userService.GetLoggedInUser();
            var currentDate = DateTime.Now;
            var period = await _context.Periods.FirstOrDefaultAsync(q => q.EndDate.Year == currentDate.Year);
            var numberOfDays = model.EndDate.DayNumber - model.StartDate.DayNumber;
            var allocation = await _context.LeaveAllocations
               .FirstAsync(q => q.LeaveTypeId == model.LeaveTypeId
               && q.EmployeeId == user.Id
               && q.PeriodId == period.Id
               );
          
            return allocation.Days < numberOfDays;
        }

        public async Task ReviewLeaveRquest(int leaveRequestId, bool approved)
        {
            var user = await _userService.GetLoggedInUser();
            var leaveRequest = await _context.LeaveRequests.FindAsync(leaveRequestId);
            leaveRequest.LeaveRequestStatusId = approved
                ? (int)LeaveRequestStatusEnum.Approved
                : (int)LeaveRequestStatusEnum.Declined;

            leaveRequest.ReviewerId = user.Id;

            if(!approved)
            {
                //restore allocation days based on requested days
                await UpdateAllocationDays(leaveRequest, false);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ReviewLeaveRequestVM> GetLeaveRequestForReview(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(q => q.LeaveType)
                .FirstAsync(q => q.Id == id);
            var user = await _userService.GetUserById(leaveRequest.EmployeeId);

            var model = new ReviewLeaveRequestVM
            {
                Id = leaveRequest.Id,
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                LeaveType = leaveRequest.LeaveType.Name,  
                RequestComments = leaveRequest.RequestComments,
                NumberOfDays = leaveRequest.EndDate.DayNumber - leaveRequest.StartDate.DayNumber,
                LeaveRequestStatus = (LeaveRequestStatusEnum)leaveRequest.LeaveRequestStatusId,
                Employee = new EmployeeListVM
                {
                    Id = leaveRequest.EmployeeId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email
                }
            };

            return model;
        }

        private async Task UpdateAllocationDays(LeaveRequest leaveRequest, bool deductDays)
        {
            var allocation = await _leaveAllocationsService.GetCurrentAllocation
                (leaveRequest.LeaveTypeId, leaveRequest.EmployeeId);
            var numberOfDays = CalculateDays(leaveRequest.StartDate, leaveRequest.EndDate);

            if(deductDays)
            {
                allocation.Days -= numberOfDays;
            }
            else
            {
                allocation.Days += numberOfDays;
            }
            _context.Entry(allocation).State = EntityState.Modified;
        }

        private int CalculateDays(DateOnly startDate, DateOnly endDate) =>
            endDate.DayNumber - startDate.DayNumber;
    }
}
