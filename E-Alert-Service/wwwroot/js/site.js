/**
 * AdvansE-Alert Client-Side Table Paginator
 * Automatically paginates any table with class .table-paginated or attribute data-paginate="true"
 */
$(document).ready(function() {
    initClientTablePaginators();
});

function initClientTablePaginators() {
    $('table.table-paginated, table[data-paginate="true"]').each(function() {
        const table = $(this);
        if (table.data('paginated-initialized')) return;
        table.data('paginated-initialized', true);

        const pageSize = parseInt(table.data('page-size') || 10, 10);
        const tbody = table.find('tbody');
        const rows = tbody.find('tr').filter(function() {
            // Exclude empty-state rows
            return !$(this).find('td[colspan]').length;
        });

        const totalRows = rows.length;
        if (totalRows <= pageSize) {
            // No pagination needed if rows <= pageSize
            return;
        }

        let currentPage = 1;
        const totalPages = Math.ceil(totalRows / pageSize);

        // Create container for pagination controls
        const paginationId = 'client-paginator-' + Math.random().toString(36).substring(2, 9);
        const container = $(`
            <div class="pagination-container" id="${paginationId}">
                <div class="d-flex align-items-center gap-3">
                    <small class="text-dim info-text"></small>
                </div>
                <nav aria-label="Table pagination">
                    <ul class="pagination pagination-sm mb-0"></ul>
                </nav>
            </div>
        `);

        // Insert container right after table or table-responsive wrapper
        if (table.parent().hasClass('table-responsive')) {
            table.parent().after(container);
        } else {
            table.after(container);
        }

        function showPage(page) {
            currentPage = page;
            const start = (page - 1) * pageSize;
            const end = start + pageSize;

            rows.hide();
            rows.slice(start, end).fadeIn(120);

            const displayStart = start + 1;
            const displayEnd = Math.min(end, totalRows);
            container.find('.info-text').html(
                `Showing <span class="fw-bold text-white">${displayStart}</span> to ` +
                `<span class="fw-bold text-white">${displayEnd}</span> of ` +
                `<span class="fw-bold text-accent">${totalRows}</span> entries`
            );

            renderPageButtons();
        }

        function renderPageButtons() {
            const ul = container.find('ul.pagination');
            ul.empty();

            // Prev button
            const prevDisabled = currentPage === 1 ? 'disabled' : '';
            ul.append(`
                <li class="page-item ${prevDisabled}">
                    <a class="page-link btn-prev" href="javascript:void(0)" aria-label="Previous">
                        <i class="bi bi-chevron-left"></i>
                    </a>
                </li>
            `);

            // Page numbers
            const startP = Math.max(1, currentPage - 2);
            const endP = Math.min(totalPages, currentPage + 2);

            if (startP > 1) {
                ul.append(`<li class="page-item"><a class="page-link btn-page" href="javascript:void(0)" data-page="1">1</a></li>`);
                if (startP > 2) {
                    ul.append(`<li class="page-item disabled"><span class="page-link">…</span></li>`);
                }
            }

            for (let i = startP; i <= endP; i++) {
                const active = i === currentPage ? 'active' : '';
                ul.append(`
                    <li class="page-item ${active}">
                        <a class="page-link btn-page" href="javascript:void(0)" data-page="${i}">${i}</a>
                    </li>
                `);
            }

            if (endP < totalPages) {
                if (endP < totalPages - 1) {
                    ul.append(`<li class="page-item disabled"><span class="page-link">…</span></li>`);
                }
                ul.append(`<li class="page-item"><a class="page-link btn-page" href="javascript:void(0)" data-page="${totalPages}">${totalPages}</a></li>`);
            }

            // Next button
            const nextDisabled = currentPage === totalPages ? 'disabled' : '';
            ul.append(`
                <li class="page-item ${nextDisabled}">
                    <a class="page-link btn-next" href="javascript:void(0)" aria-label="Next">
                        <i class="bi bi-chevron-right"></i>
                    </a>
                </li>
            `);

            // Event handlers
            ul.find('.btn-prev').on('click', function() {
                if (currentPage > 1) showPage(currentPage - 1);
            });

            ul.find('.btn-next').on('click', function() {
                if (currentPage < totalPages) showPage(currentPage + 1);
            });

            ul.find('.btn-page').on('click', function() {
                const p = parseInt($(this).data('page'), 10);
                if (p && p !== currentPage) showPage(p);
            });
        }

        // Show page 1 initially
        showPage(1);
    });
}
