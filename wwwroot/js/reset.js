function resetSearchForm() {
    const form = document.getElementById('searchForm');
    if (!form) return;

    // Clear all input fields
    form.querySelectorAll('input, select').forEach(el => {
        if (el.tagName === 'INPUT') el.value = '';
        if (el.tagName === 'SELECT') el.selectedIndex = 0;
    });

    // Submit the form immediately
    form.submit();
}
