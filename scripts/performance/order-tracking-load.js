import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

export const options = {
  scenarios: {
    dashboard_read_path: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 100 },
        { duration: '1m', target: 500 },
        { duration: '1m', target: 1000 },
        { duration: '30s', target: 0 }
      ],
      gracefulRampDown: '15s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
    checks: ['rate>0.99']
  }
};

const activeOrdersLatency = new Trend('active_orders_latency');
const successfulResponses = new Rate('successful_responses');

const baseUrl = __ENV.BASE_URL ?? 'https://localhost:7247';
const token = __ENV.TOKEN ?? '';

export default function () {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const response = http.get(`${baseUrl}/api/v1/orders/active?page=1&pageSize=100`, {
    headers,
    tags: { endpoint: 'active-orders' }
  });

  activeOrdersLatency.add(response.timings.duration);
  successfulResponses.add(response.status === 200);

  check(response, {
    'active orders returns 200': (result) => result.status === 200,
    'active orders under 500ms': (result) => result.timings.duration < 500
  });

  sleep(1);
}
