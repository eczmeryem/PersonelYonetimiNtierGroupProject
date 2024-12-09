﻿using NtierArchitecture.Business.Services;
using NtierArchitecture.DataAccess.Context;
using NtierArchitecture.DataAccess.Repositories;
using NtierArchitecture.Entities.Models;
using System.Data;


namespace NtierArchitecture.UI.Forms
{
    public partial class CalisanMaasTakibi : Form
    {
        private readonly ApplicationDbContext _context;
        private readonly EmployeeRepository _employeeRepository;
        private readonly EmployeeService _employeeService;
        private readonly DepartmentRepository _departmentRepository;
        private readonly DepartmentService _departmentService;
        private readonly SalaryTrackingRepository _salaryTrackingRepository;
        private readonly SalaryTrackingService _salaryTrackingService;

        private int selectedMonth;
        private int selectedYear;
        private DateTime dateThreshold;
        public CalisanMaasTakibi()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _employeeRepository = new EmployeeRepository(_context);
            _employeeService = new EmployeeService(_employeeRepository);
            _departmentRepository = new DepartmentRepository(_context);
            _departmentService = new DepartmentService(_departmentRepository);
            _salaryTrackingRepository = new SalaryTrackingRepository(_context);
            _salaryTrackingService = new SalaryTrackingService(_salaryTrackingRepository);

            LoadMonthsAndYears();

        }

        private void CalisanMaasTakibi_Load(object sender, EventArgs e)
        {
            LoadDepartments();
        }



        private void LoadMonthsAndYears()
        {
            cmbMonth.DataSource = Enumerable.Range(1, 12)
                .Select(i => new { Value = i, Name = new DateTime(1, i, 1).ToString("MMMM") })
                .ToList();
            cmbMonth.DisplayMember = "Name";
            cmbMonth.ValueMember = "Value";

            int currentYear = DateTime.Now.Year;
            cmbYear.DataSource = Enumerable.Range(currentYear - 5, 6).ToList();

            cmbMonth.SelectedValue = DateTime.Now.Month;
            cmbYear.SelectedItem = currentYear;
        }

        private void btnGetByDate_Click(object sender, EventArgs e)
        {
            if (cmbMonth.SelectedValue is null || cmbYear.SelectedValue is null)
            {
                MessageBox.Show("Lütfen yıl ve ay seçin.");
                return;
            }

            selectedMonth = (int)cmbMonth.SelectedValue;
            selectedYear = (int)cmbYear.SelectedValue;

            dateThreshold = new DateTime(selectedYear, selectedMonth, 5);

            var unpaidEmployees = _employeeService.GetAllEmployeesWithDepartment()
                .Where(emp =>
                    !_salaryTrackingService.IsSalaryPaid(emp.Id, selectedMonth, selectedYear) &&
                    emp.CreateDate <= dateThreshold
                )
                .Select(emp => new
                {
                    Department = emp.Department.Name,
                    Tc = emp.TcNo,
                    FullName = $"{emp.Name} {emp.Surname}",
                    Salary = emp.Salary
                })
                .ToList();

            dgwEmployees.Rows.Clear();

            foreach (var employee in unpaidEmployees)
            {
                dgwEmployees.Rows.Add(
                    employee.Department,
                    employee.Tc,
                    employee.FullName,
                    employee.Salary
                );
            }

            if (!unpaidEmployees.Any())
            {
                MessageBox.Show("Seçilen ay için maaşı ödenmemiş çalışan bulunamadı.");
            }
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            if (dgwEmployees.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen ödeme yapmak istediğiniz çalışanı seçin.");
                return;
            }

            var selectedRow = dgwEmployees.SelectedRows[0];
            string tcNumber = selectedRow.Cells["Tc"].Value.ToString();

            var employee = _employeeService.GetEmployeeByTcNo(tcNumber);
            if (employee != null)
            {
                var salaryTracking = new SalaryTracking
                {
                    IsPayed = true,
                    PaymentMonth = selectedMonth,
                    PaymentYear = selectedYear,
                    EmployeeId = employee.Id
                };
                _salaryTrackingService.Create(salaryTracking);

                MessageBox.Show($"{employee.Name} {employee.Surname} için maaş ödendi.");
                btnGetByDate.PerformClick();
            }
            else
            {
                MessageBox.Show("Seçilen çalışan bulunamadı.");
            }
        }

        private void dgwEmployees_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgwEmployees.Rows[e.RowIndex].Cells["Tc"].Value != null)
            {
                string selectedTc = dgwEmployees.Rows[e.RowIndex].Cells["Tc"].Value.ToString();

                lblPersonelTc.Text = $"Seçilen Personelin TC'si: {selectedTc}";
            }
            else
            {
                lblPersonelTc.Text = lblPersonelTc.Text;
            }
        }

        private void LoadDepartments()
        {
            var departments = _departmentService.GetAll(); 
            cmbDepartments.DataSource = departments;
            cmbDepartments.DisplayMember = "Name"; 
            cmbDepartments.ValueMember = "Id";
        }

        private void cmbDepartments_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            var selectedDepartmentId = cmbDepartments.SelectedValue as Guid?;

            if (selectedDepartmentId.HasValue)
            {
                
                var filteredEmployees = _employeeService.GetAllEmployeesWithDepartment()
                    .Where(emp => emp.DepartmentId == selectedDepartmentId.Value &&
                                  !_salaryTrackingService.IsSalaryPaid(emp.Id, (int)cmbMonth.SelectedValue, (int)cmbYear.SelectedValue) && 
                                  emp.CreateDate <= new DateTime((int)cmbYear.SelectedValue, (int)cmbMonth.SelectedValue, 5)) 
                    .Select(emp => new
                    {
                        Department = emp.Department.Name,
                        Tc = emp.TcNo,
                        FullName = $"{emp.Name} {emp.Surname}",
                        Salary = emp.Salary
                    })
                    .ToList();

                
                dgwEmployees.Rows.Clear();
                foreach (var employee in filteredEmployees)
                {
                    dgwEmployees.Rows.Add(
                        employee.Department,
                        employee.Tc,
                        employee.FullName,
                        employee.Salary
                    );
                }

                
                if (!filteredEmployees.Any())
                {
                    MessageBox.Show("Seçilen departmanda maaşı ödenmemiş çalışan bulunamadı.");
                }
            }
            else
            {
                
                LoadEmployees();
            }
        }

        private void LoadEmployees()
        {
            var unpaidEmployees = _employeeService.GetAllEmployeesWithDepartment()
                .Where(emp => !_salaryTrackingService.IsSalaryPaid(emp.Id, selectedMonth, selectedYear) &&
                             emp.CreateDate <= dateThreshold)
                .Select(emp => new
                {
                    Department = emp.Department.Name,
                    Tc = emp.TcNo,
                    FullName = $"{emp.Name} {emp.Surname}",
                    Salary = emp.Salary
                })
                .ToList();

            dgwEmployees.Rows.Clear();
            foreach (var employee in unpaidEmployees)
            {
                dgwEmployees.Rows.Add(
                    employee.Department,
                    employee.Tc,
                    employee.FullName,
                    employee.Salary
                );
            }

            
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            
            string searchText = txtSearch.Text.Trim();

            
            if (searchText.Length < 3)
            {
                LoadEmployees(); 
                return;
            }

            
            int selectedMonth = (int)cmbMonth.SelectedValue;
            int selectedYear = (int)cmbYear.SelectedValue;
            DateTime dateThreshold = new DateTime(selectedYear, selectedMonth, 5);

            
            var filteredEmployees = _employeeService.GetAllEmployeesWithDepartment()
                .Where(emp => !_salaryTrackingService.IsSalaryPaid(emp.Id, selectedMonth, selectedYear) &&
                              emp.CreateDate <= dateThreshold && 
                              (emp.Name + " " + emp.Surname).Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                              emp.TcNo.Contains(searchText)) 
                .Select(emp => new
                {
                    Department = emp.Department.Name,
                    Tc = emp.TcNo,
                    FullName = $"{emp.Name} {emp.Surname}",
                    Salary = emp.Salary
                })
                .ToList();

            
            dgwEmployees.Rows.Clear();
            foreach (var employee in filteredEmployees)
            {
                dgwEmployees.Rows.Add(
                    employee.Department,
                    employee.Tc,
                    employee.FullName,
                    employee.Salary
                );
            }

            
        }
    }
}