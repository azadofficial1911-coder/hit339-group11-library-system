namespace LibraryManagementSystem.Services
{
    // The controller will depend on this interface instead of
    // depending directly on a particular calculation class.
    public interface IFineCalculator
    {
        int CalculateLateDays(DateTime dueDate, DateTime returnDate);

        decimal CalculateFine(DateTime dueDate, DateTime returnDate);
    }
}