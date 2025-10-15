namespace LeaveManagementSystem.Web.Data
{
    public class LeaveRequest : BaseEntity
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? RequestComments { get; set; }


        // Relationships
        public int LeaveTypeId { get; set; }
        public LeaveType? LeaveType { get; set; }
        public int LeaveRequestStatusId { get; set; }
        public LeaveRequestStatus? LeaveRequestStatus { get; set; }
        public string EmployeeId { get; set; } = default!;
        public ApplicationUser? Employee { get; set; }
        public string? ReviewerId { get; set; }
        public ApplicationUser? Reviewer { get; set; }
    }
}