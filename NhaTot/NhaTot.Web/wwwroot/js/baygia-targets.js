(() => {
    const body = document.getElementById('targetRows');
    if (!body) return;
    const rows = [...body.querySelectorAll('[data-target-row]')];
    const search = document.getElementById('targetSearch');
    const availability = document.getElementById('targetAvailability');
    const sort = document.getElementById('targetSort');
    const size = document.getElementById('targetPageSize');
    let page = 0;
    const normalize = value => value.toLocaleLowerCase('vi-VN').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replaceAll('đ', 'd');
    function render(reset = false) {
        if (reset) page = 0;
        const term = normalize(search.value.trim());
        const filtered = rows.filter(row => normalize(row.textContent).includes(term)
            && (availability.value === 'all' || (availability.value === 'available' ? +row.dataset.count > 0 : +row.dataset.count === 0)));
        filtered.sort((a, b) => (sort.value === 'count' ? +b.dataset.count - +a.dataset.count : 0) || +b.dataset.order - +a.dataset.order);
        const length = size.value === 'all' ? Math.max(filtered.length, 1) : +size.value;
        const pages = Math.max(1, Math.ceil(filtered.length / length));
        page = Math.min(Math.max(page, 0), pages - 1);
        rows.forEach(row => { row.hidden = true; });
        filtered.forEach((row, index) => { body.append(row); row.hidden = index < page * length || index >= (page + 1) * length; });
        const empty = document.getElementById('targetNoResults');
        body.append(empty); empty.hidden = filtered.length !== 0;
        document.getElementById('targetPageInfo').textContent = filtered.length
            ? `${page * length + 1}–${Math.min((page + 1) * length, filtered.length)} / ${filtered.length} yêu cầu`
            : '0 yêu cầu';
        document.getElementById('targetPrev').disabled = page === 0;
        document.getElementById('targetNext').disabled = page + 1 >= pages;
    }
    search.addEventListener('input', () => render(true));
    [availability, sort, size].forEach(input => input.addEventListener('change', () => render(true)));
    document.getElementById('targetPrev').addEventListener('click', () => { page--; render(); });
    document.getElementById('targetNext').addEventListener('click', () => { page++; render(); });
    document.querySelectorAll('[data-target-detail]').forEach(panel => {
        panel.addEventListener('shown.bs.collapse', () => panel.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    });
    render();
})();
