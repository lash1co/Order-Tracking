import { expect, test } from '@playwright/test';

const orders = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    customerId: '22222222-2222-2222-2222-222222222222',
    restaurantId: '33333333-3333-3333-3333-333333333333',
    status: 'Pending',
    createdAt: new Date().toISOString(),
    estimatedDelivery: new Date(Date.now() + 30 * 60_000).toISOString(),
    actualDelivery: null,
    version: 'AAAAAAAAB9E=',
    items: [
      {
        id: '44444444-4444-4444-4444-444444444444',
        menuItemId: '55555555-5555-5555-5555-555555555555',
        quantity: 2,
        price: 18.5
      }
    ]
  }
];

test.beforeEach(async ({ page }) => {
  await page.route('**/api/v1/orders/active**', async (route) => {
    await route.fulfill({ json: orders });
  });

  await page.route('**/api/v1/orders/*/status', async (route) => {
    const body = await route.request().postDataJSON();
    await route.fulfill({
      json: {
        ...orders[0],
        status: body.status,
        version: 'AAAAAAAAB9F='
      }
    });
  });
});

test('dashboard renders KPIs and supports optimistic status updates', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Order Tracking System' })).toBeVisible();
  await expect(page.getByText('Órdenes activas')).toBeVisible();
  await expect(page.getByText('#11111111')).toBeVisible();

  await page.getByRole('button', { name: /Avanzar a Preparing/i }).click();

  await expect(page.getByText('Preparing')).toBeVisible();
  await expect(page.getByText(/actualizada/i)).toBeVisible();
});
