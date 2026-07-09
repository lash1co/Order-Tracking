{{- define "order-tracking.name" -}}
order-tracking
{{- end -}}

{{- define "order-tracking.namespace" -}}
{{- default .Release.Namespace .Values.namespaceOverride -}}
{{- end -}}
