import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    signalr_negotiate: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS ?? 50),
      duration: __ENV.DURATION ?? '1m'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<500'],
    checks: ['rate>0.95']
  }
};

const baseUrl = __ENV.BASE_URL ?? 'https://localhost:7247';
const token = __ENV.TOKEN ?? '';

export default function () {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const response = http.post(`${baseUrl}/hubs/tracking/negotiate?negotiateVersion=1`, null, {
    headers,
    tags: { endpoint: 'signalr-negotiate' }
  });

  check(response, {
    'negotiate succeeds with token or rejects anonymous': (result) => [200, 401].includes(result.status),
    'negotiate under 500ms': (result) => result.timings.duration < 500
  });

  sleep(1);
}
