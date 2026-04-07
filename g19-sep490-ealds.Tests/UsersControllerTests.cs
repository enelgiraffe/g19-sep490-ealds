using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class UsersControllerTests : IDisposable
{
    private readonly TestEaldsDbContext _context;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<EaldsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestEaldsDbContext(options);
        _controller = new UsersController(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetUsers Tests

    /// <summary>
    /// Test: GET /api/Users returns all active users
    /// Expected: 200 OK with IEnumerable&lt;UserDTO&gt; containing all active users
    /// </summary>
    [Fact]
    public async Task GetUsers_ReturnsAllActiveUsers()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        var user1 = new User
        {
            UserId = 1,
            Email = "user1@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var user2 = new User
        {
            UserId = 2,
            Email = "user2@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee1 = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "John Doe",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        var employee2 = new Employee
        {
            EmployeeId = 2,
            UserId = 2,
            DepartmentId = 1,
            Name = "Jane Smith",
            Code = "EMP002",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Departments.Add(department);
        _context.Users.AddRange(user1, user2);
        _context.Employees.AddRange(employee1, employee2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        var users = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value);
        var userList = users.ToList();

        Assert.Equal(2, userList.Count);
    }

    /// <summary>
    /// Test: GET /api/Users returns empty list when no users exist
    /// Expected: 200 OK with empty IEnumerable&lt;UserDTO&gt;
    /// </summary>
    [Fact]
    public async Task GetUsers_WithNoUsers_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        var users = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value);
        Assert.Empty(users);
    }

    /// <summary>
    /// Test: GET /api/Users excludes deactivated users
    /// Expected: 200 OK with IEnumerable&lt;UserDTO&gt; containing only active users (Status = 1)
    /// </summary>
    [Fact]
    public async Task GetUsers_ExcludesDeactivatedUsers()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        var activeUser = new User
        {
            UserId = 1,
            Email = "active@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var deactivatedUser = new User
        {
            UserId = 2,
            Email = "deactivated@test.com",
            Password = "hashedpassword",
            Status = 0
        };

        var employee1 = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Active User",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        var employee2 = new Employee
        {
            EmployeeId = 2,
            UserId = 2,
            DepartmentId = 1,
            Name = "Deactivated User",
            Code = "EMP002",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Departments.Add(department);
        _context.Users.AddRange(activeUser, deactivatedUser);
        _context.Employees.AddRange(employee1, employee2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value);
        var userList = users.ToList();

        Assert.Single(userList);
        Assert.Equal("active@test.com", userList[0].Email);
        Assert.DoesNotContain(userList, u => u.Email == "deactivated@test.com");
    }

    /// <summary>
    /// Test: GET /api/Users returns user with roles and department
    /// Expected: 200 OK with IEnumerable&lt;UserDTO&gt; where each user has RoleIds and Roles populated
    /// </summary>
    [Fact]
    public async Task GetUsers_ReturnsUsersWithRolesAndDepartment()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        var role1 = new Role
        {
            RoleId = 1,
            Name = "Admin",
            Code = "ADMIN",
            CreateDate = DateTime.Now
        };

        var role2 = new Role
        {
            RoleId = 2,
            Name = "Manager",
            Code = "MGR",
            CreateDate = DateTime.Now
        };

        var user = new User
        {
            UserId = 1,
            Email = "admin@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Admin User",
            Code = "ADM001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Departments.Add(department);
        _context.Roles.AddRange(role1, role2);
        _context.Users.Add(user);
        _context.Employees.Add(employee);
        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<UserDTO>>(okResult.Value);
        var userList = users.ToList();

        Assert.Single(userList);
        Assert.Equal(2, userList[0].RoleIds.Count);
        Assert.Contains("Admin", userList[0].Roles);
        Assert.Contains("Manager", userList[0].Roles);
        Assert.Equal("IT Department", userList[0].DepartmentName);
        Assert.Equal("Admin User", userList[0].FullName);
    }

    #endregion

    #region GetUser Tests

    /// <summary>
    /// Test: GET /api/Users/{id} with valid ID returns user details
    /// Expected: 200 OK with UserDTO containing all fields including RoleIds, Roles, DepartmentName
    /// </summary>
    [Fact]
    public async Task GetUser_WithValidId_ReturnsUserDetails()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "HR Department",
            CreateDate = DateTime.Now
        };

        var role = new Role
        {
            RoleId = 1,
            Name = "Manager",
            Code = "MGR",
            CreateDate = DateTime.Now
        };

        var user = new User
        {
            UserId = 1,
            Email = "manager@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "HR Manager",
            Code = "HR001",
            Phone = "0912345678",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Departments.Add(department);
        _context.Roles.Add(role);
        _context.Users.Add(user);
        _context.Employees.Add(employee);
        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUser(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        var userDto = Assert.IsType<UserDTO>(okResult.Value);
        Assert.Equal(1, userDto.UserId);
        Assert.Equal("manager@test.com", userDto.Email);
        Assert.Equal("HR Manager", userDto.FullName);
        Assert.Equal("HR001", userDto.EmployeeCode);
        Assert.Equal("0912345678", userDto.Phone);
        Assert.Equal("HR Department", userDto.DepartmentName);
        Assert.Single(userDto.RoleIds);
        Assert.Contains("Manager", userDto.Roles);
    }

    /// <summary>
    /// Test: GET /api/Users/{id} with invalid ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetUser(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// Test: GET /api/Users/{id} with negative ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetUser_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetUser(-1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// Test: GET /api/Users/{id} with user that has no employee record returns user with null employee fields
    /// Expected: 200 OK with UserDTO where EmployeeCode, FullName, etc. are null
    /// </summary>
    [Fact]
    public async Task GetUser_WithNoEmployeeRecord_ReturnsUserWithNullEmployeeFields()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "orphan@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUser(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var userDto = Assert.IsType<UserDTO>(okResult.Value);

        Assert.Equal(1, userDto.UserId);
        Assert.Equal("orphan@test.com", userDto.Email);
        Assert.Null(userDto.FullName);
        Assert.Null(userDto.EmployeeCode);
        Assert.Null(userDto.DepartmentName);
    }

    #endregion

    #region CreateUser Tests

    /// <summary>
    /// Test: POST /api/Users with valid data creates user, employee, and roles
    /// Expected: 201 Created with UserDTO containing all fields and AssetTypeCount=0
    /// </summary>
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        var role = new Role
        {
            RoleId = 1,
            Name = "Admin",
            Code = "ADMIN",
            CreateDate = DateTime.Now
        };

        _context.Departments.Add(department);
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var dto = new CreateUserDTO
        {
            FullName = "John Doe",
            EmployeeCode = "EMP001",
            Email = "john.doe@test.com",
            Password = "Password123!",
            DepartmentId = 1,
            Phone = "0912345678",
            Status = 1,
            RoleIds = new List<int> { 1 }
        };

        // Act
        var result = await _controller.CreateUser(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetUser", createdResult.ActionName);

        var userDto = Assert.IsType<UserDTO>(createdResult.Value);
        Assert.True(userDto.UserId > 0);
        Assert.Equal("john.doe@test.com", userDto.Email);
        Assert.Equal("John Doe", userDto.FullName);
        Assert.Equal("EMP001", userDto.EmployeeCode);
        Assert.Equal("IT Department", userDto.DepartmentName);

        // Verify database
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@test.com");
        Assert.NotNull(savedUser);
        Assert.Equal(1, savedUser.Status);

        var savedEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == savedUser.UserId);
        Assert.NotNull(savedEmployee);
        Assert.Equal("John Doe", savedEmployee.Name);
        Assert.Equal("EMP001", savedEmployee.Code);
    }

    /// <summary>
    /// Test: POST /api/Users with duplicate email returns 400
    /// Expected: 400 Bad Request with error message about duplicate email
    /// </summary>
    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User
        {
            UserId = 1,
            Email = "existing@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var dto = new CreateUserDTO
        {
            FullName = "New User",
            EmployeeCode = "EMP002",
            Email = "existing@test.com",
            Password = "Password123!",
            DepartmentId = 1,
            Status = 1
        };

        // Act
        var result = await _controller.CreateUser(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: POST /api/Users with invalid department ID returns 400
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateUser_WithInvalidDepartmentId_ReturnsBadRequest()
    {
        // Act
        var dto = new CreateUserDTO
        {
            FullName = "John Doe",
            EmployeeCode = "EMP001",
            Email = "john.doe@test.com",
            Password = "Password123!",
            DepartmentId = 999,
            Status = 1
        };

        var result = await _controller.CreateUser(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: POST /api/Users with duplicate employee code returns 400
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateUser_WithDuplicateEmployeeCode_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "user1@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Existing Employee",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Users.Add(user);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var dto = new CreateUserDTO
        {
            FullName = "New Employee",
            EmployeeCode = "EMP001",
            Email = "new.employee@test.com",
            Password = "Password123!",
            DepartmentId = 1,
            Status = 1
        };

        // Act
        var result = await _controller.CreateUser(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: POST /api/Users with spaces in FullName (should be trimmed)
    /// Expected: 201 Created with trimmed FullName in UserDTO
    /// </summary>
    [Fact]
    public async Task CreateUser_WithSpacesInFullName_TrimsFullName()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        var dto = new CreateUserDTO
        {
            FullName = "  John Doe  ",
            EmployeeCode = "EMP001",
            Email = "john.doe@test.com",
            Password = "Password123!",
            DepartmentId = 1,
            Status = 1
        };

        // Act
        var result = await _controller.CreateUser(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var userDto = Assert.IsType<UserDTO>(createdResult.Value);
        Assert.Equal("John Doe", userDto.FullName);
        Assert.DoesNotContain("  ", userDto.FullName!);
    }

    /// <summary>
    /// Test: POST /api/Users with no roles creates user without roles
    /// Expected: 201 Created with UserDTO where RoleIds is empty
    /// </summary>
    [Fact]
    public async Task CreateUser_WithNoRoles_CreatesUserWithoutRoles()
    {
        // Arrange
        var department = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        var dto = new CreateUserDTO
        {
            FullName = "No Role User",
            EmployeeCode = "EMP001",
            Email = "norole@test.com",
            Password = "Password123!",
            DepartmentId = 1,
            Status = 1,
            RoleIds = new List<int>()
        };

        // Act
        var result = await _controller.CreateUser(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var userDto = Assert.IsType<UserDTO>(createdResult.Value);
        Assert.Empty(userDto.RoleIds);
    }

    #endregion

    #region UpdateUser Tests

    /// <summary>
    /// Test: PUT /api/Users/{id} with valid data updates user, employee, and roles
    /// Expected: 204 No Content
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var department1 = new Department
        {
            DepartmentId = 1,
            Name = "IT Department",
            CreateDate = DateTime.Now
        };

        var department2 = new Department
        {
            DepartmentId = 2,
            Name = "HR Department",
            CreateDate = DateTime.Now
        };

        var role = new Role
        {
            RoleId = 1,
            Name = "Manager",
            Code = "MGR",
            CreateDate = DateTime.Now
        };

        var user = new User
        {
            UserId = 1,
            Email = "user@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "Old Name",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Departments.AddRange(department1, department2);
        _context.Roles.Add(role);
        _context.Users.Add(user);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDTO
        {
            FullName = "Updated Name",
            Email = "updated@test.com",
            Phone = "0999888777",
            DepartmentId = 2,
            Status = 1,
            RoleIds = new List<int> { 1 }
        };

        // Act
        var result = await _controller.UpdateUser(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result.Result);

        // Verify database
        var updatedUser = await _context.Users.FindAsync(1);
        Assert.NotNull(updatedUser);
        Assert.Equal("updated@test.com", updatedUser.Email);

        var updatedEmployee = await _context.Employees.FindAsync(1);
        Assert.NotNull(updatedEmployee);
        Assert.Equal("Updated Name", updatedEmployee.Name);
        Assert.Equal("0999888777", updatedEmployee.Phone);
        Assert.Equal(2, updatedEmployee.DepartmentId);
    }

    /// <summary>
    /// Test: PUT /api/Users/{id} with invalid user ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateUserDTO
        {
            FullName = "Updated Name",
            Email = "updated@test.com",
            Phone = "0999888777",
            DepartmentId = 1,
            Status = 1,
            RoleIds = new List<int>()
        };

        // Act
        var result = await _controller.UpdateUser(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: PUT /api/Users/{id} with duplicate email returns 400
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var user1 = new User
        {
            UserId = 1,
            Email = "user1@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var user2 = new User
        {
            UserId = 2,
            Email = "user2@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 2,
            DepartmentId = 1,
            Name = "User 2",
            Code = "EMP002",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Users.AddRange(user1, user2);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDTO
        {
            FullName = "User 1",
            Email = "user1@test.com",
            Phone = "0912345678",
            DepartmentId = 1,
            Status = 1,
            RoleIds = new List<int>()
        };

        // Act - Try to update user2's email to user1's email
        var result = await _controller.UpdateUser(2, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: PUT /api/Users/{id} with invalid department ID returns 400
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithInvalidDepartmentId_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "user@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "User",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Users.Add(user);
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDTO
        {
            FullName = "Updated Name",
            Email = "updated@test.com",
            Phone = "0999888777",
            DepartmentId = 999,
            Status = 1,
            RoleIds = new List<int>()
        };

        // Act
        var result = await _controller.UpdateUser(1, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: PUT /api/Users/{id} updates roles correctly
    /// Expected: 204 No Content and roles are updated in database
    /// </summary>
    [Fact]
    public async Task UpdateUser_UpdatesRoles_ReturnsNoContent()
    {
        // Arrange
        var role1 = new Role { RoleId = 1, Name = "Admin", Code = "ADMIN", CreateDate = DateTime.Now };
        var role2 = new Role { RoleId = 2, Name = "Manager", Code = "MGR", CreateDate = DateTime.Now };

        var user = new User
        {
            UserId = 1,
            Email = "user@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "User",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Roles.AddRange(role1, role2);
        _context.Users.Add(user);
        _context.Employees.Add(employee);
        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDTO
        {
            FullName = "User",
            Email = "user@test.com",
            Phone = "0912345678",
            DepartmentId = 1,
            Status = 1,
            RoleIds = new List<int> { 2 }
        };

        // Act
        var result = await _controller.UpdateUser(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result.Result);

        var userRoles = await _context.UserRoles.Where(ur => ur.UserId == 1).ToListAsync();
        Assert.Single(userRoles);
        Assert.Equal(2, userRoles[0].RoleId);
    }

    #endregion

    #region DeactivateUser Tests

    /// <summary>
    /// Test: PUT /api/Users/{id}/deactivate with valid ID sets Status to 0
    /// Expected: 204 No Content and user Status is 0 in database
    /// </summary>
    [Fact]
    public async Task DeactivateUser_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "active@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeactivateUser(1);

        // Assert
        Assert.IsType<NoContentResult>(result.Result);

        var deactivatedUser = await _context.Users.FindAsync(1);
        Assert.NotNull(deactivatedUser);
        Assert.Equal(0, deactivatedUser.Status);
    }

    /// <summary>
    /// Test: PUT /api/Users/{id}/deactivate with invalid ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task DeactivateUser_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeactivateUser(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: PUT /api/Users/{id}/deactivate with negative ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task DeactivateUser_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeactivateUser(-1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region ManageUserRoles Tests

    /// <summary>
    /// Test: POST /api/Users/{id}/roles with valid data replaces user roles
    /// Expected: 200 OK with updated roles
    /// </summary>
    [Fact]
    public async Task ManageUserRoles_WithValidData_ReturnsOk()
    {
        // Arrange
        var role1 = new Role { RoleId = 1, Name = "Admin", Code = "ADMIN", CreateDate = DateTime.Now };
        var role2 = new Role { RoleId = 2, Name = "Manager", Code = "MGR", CreateDate = DateTime.Now };
        var role3 = new Role { RoleId = 3, Name = "Staff", Code = "STAFF", CreateDate = DateTime.Now };

        var user = new User
        {
            UserId = 1,
            Email = "user@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        _context.Roles.AddRange(role1, role2, role3);
        _context.Users.Add(user);
        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await _context.SaveChangesAsync();

        var dto = new AssignRoleDTO { RoleIds = new List<int> { 2, 3 } };

        // Act
        var result = await _controller.ManageUserRoles(1, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        var userRoles = await _context.UserRoles.Where(ur => ur.UserId == 1).ToListAsync();
        Assert.Equal(2, userRoles.Count);
        Assert.Contains(userRoles, ur => ur.RoleId == 2);
        Assert.Contains(userRoles, ur => ur.RoleId == 3);
        Assert.DoesNotContain(userRoles, ur => ur.RoleId == 1);
    }

    /// <summary>
    /// Test: POST /api/Users/{id}/roles with invalid user ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task ManageUserRoles_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        var dto = new AssignRoleDTO { RoleIds = new List<int> { 1 } };

        // Act
        var result = await _controller.ManageUserRoles(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region DeleteUser Tests

    /// <summary>
    /// Test: DELETE /api/Users/{id} with valid ID deletes user, employee, and roles
    /// Expected: 204 No Content and all related records are removed from database
    /// </summary>
    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "todelete@test.com",
            Password = "hashedpassword",
            Status = 1
        };

        var employee = new Employee
        {
            EmployeeId = 1,
            UserId = 1,
            DepartmentId = 1,
            Name = "To Delete",
            Code = "EMP001",
            Status = 1,
            CreateDate = DateTime.Now,
            CreatedBy = 1
        };

        _context.Users.Add(user);
        _context.Employees.Add(employee);
        _context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteUser(1);

        // Assert
        Assert.IsType<NoContentResult>(result.Result);

        var deletedUser = await _context.Users.FindAsync(1);
        Assert.Null(deletedUser);

        var deletedEmployee = await _context.Employees.FindAsync(1);
        Assert.Null(deletedEmployee);
    }

    /// <summary>
    /// Test: DELETE /api/Users/{id} with invalid ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task DeleteUser_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteUser(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>
    /// Test: DELETE /api/Users/{id} with negative ID returns 404
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task DeleteUser_WithNegativeId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteUser(-1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion
}

/// <summary>
/// Custom DbContext for unit testing.
/// Configures UserRole with a composite key so it works with EF Core InMemory provider.
/// </summary>
public class TestEaldsDbContext : EaldsDbContext
{
    public TestEaldsDbContext(DbContextOptions<EaldsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Override UserRole to have a composite primary key so InMemory provider can track it
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        });
    }
}
