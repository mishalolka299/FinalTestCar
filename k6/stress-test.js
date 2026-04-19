import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5202';
const errorRate = new Rate('errors');

export const options = {
    scenarios: {
        concurrent_reservations: {
            executor: 'ramping-vus',
            startVUs: 10,
            stages: [
                { duration: '1m', target: 50 },
                { duration: '2m', target: 100 },
                { duration: '2m', target: 200 },
                { duration: '2m', target: 200 },
                { duration: '1m', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<2000'],
        http_req_failed: ['rate<0.05'],
        errors: ['rate<0.05'],
    },
};

export default function () {
    group('Concurrent reservations stress', () => {
        // GET list of available cars
        const listRes = http.get(`${BASE_URL}/api/cars?status=Available`);
        check(listRes, { 'GET cars 200': (r) => r.status === 200 });

        let cars = [];
        try { cars = listRes.json(); } catch { }

        if (Array.isArray(cars) && cars.length > 0) {
            const car = cars[Math.floor(Math.random() * cars.length)];
            const reserveRes = http.request('PATCH', `${BASE_URL}/api/cars/${car.id}/reserve`, null, {
                headers: { 'Content-Type': 'application/json' },
            });

            // Under concurrent stress, 200 (reserved) or 409 (already reserved) are both acceptable
            const ok = check(reserveRes, {
                'reserve 200 or 409': (r) => r.status === 200 || r.status === 409,
            });
            errorRate.add(!ok);
        }
    });

    sleep(0.5);
}
