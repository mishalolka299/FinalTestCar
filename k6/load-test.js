import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5202';

const errorRate = new Rate('errors');
const searchTrend = new Trend('search_duration');
const filterTrend = new Trend('filter_duration');

export const options = {
    scenarios: {
        load: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '30s', target: 50 },
                { duration: '4m', target: 50 },
                { duration: '30s', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_duration: ['p(50)<300', 'p(95)<800', 'p(99)<2000'],
        http_req_failed: ['rate<0.01'],
        errors: ['rate<0.01'],
    },
};

export default function () {
    group('Car search with multiple filters', () => {
        const makes = ['Toyota', 'Honda', 'BMW', 'Ford', 'Audi'];
        const fuelTypes = ['Petrol', 'Diesel', 'Electric', 'Hybrid'];
        const make = makes[Math.floor(Math.random() * makes.length)];
        const fuel = fuelTypes[Math.floor(Math.random() * fuelTypes.length)];
        const yearFrom = 2000 + Math.floor(Math.random() * 15);
        const yearTo = yearFrom + Math.floor(Math.random() * 5) + 1;
        const priceFrom = Math.floor(Math.random() * 10000) + 5000;
        const priceTo = priceFrom + Math.floor(Math.random() * 20000) + 5000;

        const url = `${BASE_URL}/api/cars?make=${make}&yearFrom=${yearFrom}&yearTo=${yearTo}&priceFrom=${priceFrom}&priceTo=${priceTo}&fuelType=${fuel}&status=Available`;
        const res = http.get(url);
        filterTrend.add(res.timings.duration);

        const ok = check(res, {
            'GET /api/cars with filters status 200': (r) => r.status === 200,
            'response is array': (r) => { try { return Array.isArray(r.json()); } catch { return false; } },
        });
        errorRate.add(!ok);
    });

    sleep(1);

    group('Car search by make only', () => {
        const res = http.get(`${BASE_URL}/api/cars?make=Toyota`);
        searchTrend.add(res.timings.duration);

        const ok = check(res, {
            'GET /api/cars?make=Toyota status 200': (r) => r.status === 200,
        });
        errorRate.add(!ok);
    });

    sleep(1);
}
