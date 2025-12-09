// -----------------------------
// UTILITY: generate colors for pie slices
// -----------------------------
function generateColors(n) {
    const palette = [
        '#3b82f6', '#10b981', '#ef4444', '#f59e0b', '#8b5cf6',
        '#06b6d4', '#f97316', '#eab308', '#14b8a6', '#6366f1'
    ];
    return Array.from({ length: n }, (_, i) => palette[i % palette.length]);
}

// -----------------------------
// RENDER PIE CHART
// -----------------------------
function renderInteractivePieChart(canvasId, labels, data, chartTitle = "") {
    if (!document.getElementById(canvasId)) return;

    new Chart(document.getElementById(canvasId), {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: generateColors(data.length),
                borderColor: '#fff',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'bottom' },
                title: { display: !!chartTitle, text: chartTitle },
                tooltip: {
                    callbacks: {
                        label: ctx => ctx.label + ': ' + ctx.parsed
                    }
                }
            }
        }
    });
}

// -----------------------------
// RENDER ALL STATIC CHARTS
// -----------------------------
function renderAllCharts() {
    renderInteractivePieChart("topBorrowedChart", Object.keys(window.DataReport.TopBorrowedItems), Object.values(window.DataReport.TopBorrowedItems), "Top Borrowed Items");
    renderInteractivePieChart("borrowStatusChart", Object.keys(window.DataReport.BorrowStatusTotals), Object.values(window.DataReport.BorrowStatusTotals), "Borrow Status");
    renderInteractivePieChart("returnRequestChart", Object.keys(window.DataReport.ReturnRequestTotals), Object.values(window.DataReport.ReturnRequestTotals), "Return Requests");
    renderInteractivePieChart("returnConditionChart", Object.keys(window.DataReport.ReturnConditionTotals), Object.values(window.DataReport.ReturnConditionTotals), "Return Conditions");
    renderInteractivePieChart("userActivityChart", Object.keys(window.DataReport.UserActivityTotals), Object.values(window.DataReport.UserActivityTotals), "User Activity");
    renderInteractivePieChart("lowStockChart", Object.keys(window.DataReport.LowStockItems), Object.values(window.DataReport.LowStockItems), "Low Stock Items");
}

// -----------------------------
// FULL LIST TOGGLE
// -----------------------------
function setupFullListToggle() {
    const toggleBtn = document.getElementById("toggleFullList");
    const fullListTable = document.getElementById("fullListTable");
    if (toggleBtn && fullListTable) {
        toggleBtn.addEventListener("click", () => {
            fullListTable.style.display = fullListTable.style.display === "none" ? "table" : "none";
        });
    }
}

// -----------------------------
// COMBINED TOP BORROWED (All-Time / Month-Year)
// -----------------------------
let combinedChart = null;

async function fetchCombinedData(month, year) {
    try {
        const url = (month && year) 
            ? `/Admin/DataReport?handler=MonthData&month=${month}&year=${year}` 
            : `/Admin/DataReport?handler=AllTimeData`;
        const response = await fetch(url);
        const json = await response.json();
        return json;
    } catch (err) {
        console.error("Failed to fetch combined data:", err);
        return { top10: {}, fullList: {} };
    }
}

async function renderCombinedChart(month, year) {
    const data = await fetchCombinedData(month, year);
    const labels = Object.keys(data.top10);
    const values = Object.values(data.top10);

    const canvas = document.getElementById("combinedTopBorrowedChart");
    if (!canvas) return;

    if (combinedChart) combinedChart.destroy();

    combinedChart = new Chart(canvas, {
        type: 'pie',
        data: {
            labels: labels.length ? labels : ["No data"],
            datasets: [{
                data: values.length ? values : [1],
                backgroundColor: generateColors(labels.length || 1),
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: "bottom" },
                title: { display: true, text: "Top Borrowed Items (All-Time / Selected Month)" }
            }
        }
    });

    // Fill full list table
    const tableBody = document.querySelector("#fullListTable tbody");
    if (!tableBody) return;
    tableBody.innerHTML = "";
    const fullData = data.fullList || {};
    if (!Object.keys(fullData).length) tableBody.innerHTML = `<tr><td colspan="2">No items</td></tr>`;
    else Object.entries(fullData).forEach(([item, qty]) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `<td>${item}</td><td>${qty}</td>`;
        tableBody.appendChild(tr);
    });
}

function setupCombinedSelectors() {
    const monthSelect = document.getElementById("monthSelect");
    const yearSelect = document.getElementById("yearSelect");
    if (!monthSelect || !yearSelect) return;

    function updateChart() {
        const month = monthSelect.value === "all" ? null : monthSelect.value;
        const year = yearSelect.value;
        renderCombinedChart(month, year);
    }

    monthSelect.addEventListener("change", updateChart);
    yearSelect.addEventListener("change", updateChart);
}

function populateDetailsTable(id, data) {
    const tbody = document.querySelector(`#${id} tbody`);
    tbody.innerHTML = '';

    data.forEach(row => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${row.user}</td>
            <td>${row.item}</td>
            <td>${row.quantity ?? row.qty ?? ''}</td>
            <td>${row.date ?? ''}</td>
        `;
        tbody.appendChild(tr);
    });
}

function toggleTable(id) {
    const table = document.getElementById(id);
    table.style.display = table.style.display === 'none' ? '' : 'none';
}

// -----------------------------
// DOMContentLoaded
// -----------------------------
document.addEventListener("DOMContentLoaded", () => {
    renderAllCharts();
    setupFullListToggle();
    setupCombinedSelectors();

    const now = new Date();
    renderCombinedChart(now.getMonth() + 1, now.getFullYear());
});
