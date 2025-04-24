var dataTable;
$(document).ready(function () {
    loadDataTable();
})

function loadDataTable() {
    dataTable = $('#datatable').DataTable({
        "ajax": { url:'/admin/user/getall'}
    ,
        "columns": [
            { data: 'name', "width": "15%"},
            { data: 'email', "width": "15%" },
            { data: 'phoneNumber', "width": "15%" },
            { data: 'company.name', "width": "10%" },
            { data: 'role', "width": "10%" },
            {
                data: { id: 'id', lockoutEnd: "lockoutEnd"},
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockoutEnd = new Date(data.lockoutEnd).getTime();

                    if (lockoutEnd > today) {
                        return `
                        <div class="text-center">
                            <a onclick=LockUnlock('${data.id}') class="btn btn-danger text-white" style="cursor:pointer; width: 100px;">
                                <i class="bi bi-unlock-fill"></i> lock
                            </a>
                            <a href="/Admin/User/RoleManagement?userId=${data.id}" class="btn btn-danger text-white" style="cursor:pointer; width: 150px;">
                                <i class="bi bi-pencil-square"></i> Permission
                            </a>
                        </div>
                    `;
                    }
                    else {
                        return `
                        <div class="text-center">
                            <a onclick=LockUnlock('${data.id}') class="btn btn-success text-white" style="cursor:pointer; width: 100px;">
                                <i class="bi bi-unlock-fill"></i> Unlock
                            </a>
                            <a href="/Admin/User/RoleManagement?userId=${data.id}" class="btn btn-danger text-white" style="cursor:pointer; width: 150px;">
                                <i class="bi bi-pencil-square"></i> Permission
                            </a>
                        </div>
                    `;
                    }
                    
                }, "width": "25%"
            }
        ]
    });
}

function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: "/Admin/User/LockUnlock",
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (response) {
            if (response.success) {
                toastr.success(response.message);
                dataTable.ajax.reload();
            }
        }
    });
}

