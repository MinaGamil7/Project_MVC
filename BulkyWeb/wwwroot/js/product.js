﻿var dataTable;
$(document).ready(function () {
    loadDataTable();
})

function loadDataTable() {
    dataTable = $('#datatable').DataTable({
        "ajax": { url:'/admin/product/getall'}
    ,
        "columns": [
            { data: 'title', "width": "10%"},
            { data: 'isbn', "width": "10%" },
            { data: 'author', "width": "10%" },
            { data: 'description', "width": "25%" },
            { data: 'price', "width": "10%" },
            { data: 'category.name', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="text-center btn-group" role="group">
                            <a href="/Admin/Product/Upsert?id=${data}" class="btn btn-primary text-white mx-1" style="cursor:pointer; width: 100px;">
                                <i class="bi bi-pencil-square"></i> Edit
                            </a>
                            <a onClick=Delete("/admin/product/delete/${data}") class="btn btn-danger text-white mx-1" style="cursor:pointer; width: 100px;">
                                <i class="bi bi-trash"></i> Delete
                            </a>
                        </div>
                    `;
                }, "width": "20%"
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    toastr.success(data.message);
                    dataTable.ajax.reload();
                }
            });
        }
    });
}

