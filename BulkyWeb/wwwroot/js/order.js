var dataTable;
$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else if (url.includes("completed")) {
        loadDataTable("completed");
    }
    else if (url.includes("pending")) {
        loadDataTable("pending");
    }
    else if (url.includes("approved")) {
        loadDataTable("approved");
    }
    else {
        loadDataTable('all');
    }
})

function loadDataTable(status) {
    dataTable = $('#datatable').DataTable({
        "ajax": { url: `/admin/order/getall?status=${status}` }
    ,
        "columns": [
            { data: 'id', "width": "10%"},
            { data: 'name', "width": "20%" },
            { data: 'phoneNumber', "width": "10%" },
            { data: 'applicationUser.email', "width": "20%" },
            { data: 'orderStatus', "width": "10%" },
            { data: 'orderTotal', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="text-center btn-group" role="group">
                            <a href="/Admin/Order/Details?OrderId=${data}" class="btn btn-primary text-white mx-1" style="cursor:pointer; width: 100px;">
                                <i class="bi bi-pencil-square"></i>
                            </a>
                        </div>
                    `;
                }, "width": "10%"
            }
        ]
    });
}

