param(
    [string]$Namespace = "order-tracking"
)

$ErrorActionPreference = "Stop"

kubectl apply -f kubernetes/manifests/00-namespace.yaml
kubectl apply -n $Namespace -f kubernetes/manifests/01-config.yaml
kubectl apply -n $Namespace -f kubernetes/manifests/02-dependencies.yaml
kubectl apply -n $Namespace -f kubernetes/manifests/03-api.yaml
kubectl apply -n $Namespace -f kubernetes/manifests/04-ui.yaml
kubectl apply -n $Namespace -f kubernetes/manifests/05-ingress.yaml
