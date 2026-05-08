using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.FileSystem;

public sealed class StudentListService : IStudentListService
{
    public async Task<IReadOnlySet<Student>> LoadStudentsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Student list was not found.", filePath);
        }

        HashSet<Student> students = new();
        await foreach (string line in File.ReadLinesAsync(filePath, cancellationToken))
        {
            string value = line.Trim();
            if (value.Length == 0 || value.StartsWith('#'))
            {
                continue;
            }

            students.Add(new Student(value));
        }

        return students;
    }
}
