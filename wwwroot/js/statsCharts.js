// Chart rendering for Stats page
let playCountChart = null;
let listeningTimeChart = null;

window.renderCharts = function (labels, playCounts, listeningMins) {
    if (!labels || labels.length === 0) return;

    const colors = [
        '#e94590', '#7b2ff7', '#4facfe', '#43e97b',
        '#f5576c', '#f093fb', '#00f2fe', '#38f9d7'
    ];

    const bgColors = labels.map((_, i) => colors[i % colors.length]);
    const borderColors = bgColors.map(c => c + '99');

    // Play Count Bar Chart
    const ctx1 = document.getElementById('playCountChart');
    if (ctx1) {
        if (playCountChart) playCountChart.destroy();
        playCountChart = new Chart(ctx1, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Play Count',
                    data: playCounts,
                    backgroundColor: bgColors.map(c => c + '99'),
                    borderColor: bgColors,
                    borderWidth: 2,
                    borderRadius: 8,
                    borderSkipped: false,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: 'rgba(18, 18, 26, 0.95)',
                        titleColor: '#f0eef6',
                        bodyColor: '#f0eef6',
                        borderColor: 'rgba(255,255,255,0.1)',
                        borderWidth: 1,
                        cornerRadius: 12,
                        padding: 12,
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            color: 'rgba(240,238,246,0.4)',
                            stepSize: 1,
                            font: { family: 'Outfit' }
                        },
                        grid: { color: 'rgba(255,255,255,0.04)' }
                    },
                    x: {
                        ticks: {
                            color: 'rgba(240,238,246,0.6)',
                            font: { family: 'Outfit', size: 11 },
                            maxRotation: 0
                        },
                        grid: { display: false }
                    }
                }
            }
        });
    }

    // Listening Time Doughnut Chart
    const ctx2 = document.getElementById('listeningTimeChart');
    if (ctx2) {
        if (listeningTimeChart) listeningTimeChart.destroy();
        listeningTimeChart = new Chart(ctx2, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: listeningMins,
                    backgroundColor: bgColors.map(c => c + 'CC'),
                    borderColor: 'rgba(10, 10, 15, 0.8)',
                    borderWidth: 3,
                    hoverOffset: 12,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: 'rgba(240,238,246,0.7)',
                            font: { family: 'Outfit', size: 12 },
                            padding: 20,
                            usePointStyle: true,
                            pointStyleWidth: 10,
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(18, 18, 26, 0.95)',
                        titleColor: '#f0eef6',
                        bodyColor: '#f0eef6',
                        borderColor: 'rgba(255,255,255,0.1)',
                        borderWidth: 1,
                        cornerRadius: 12,
                        padding: 12,
                        callbacks: {
                            label: function (ctx) {
                                return ctx.label + ': ' + ctx.parsed.toFixed(1) + ' min';
                            }
                        }
                    }
                }
            }
        });
    }
};
