resource "tls_private_key" "rabbitmq_debug_ca_key" {
  algorithm   = "ECDSA"
  ecdsa_curve = "P256"
}

resource "tls_private_key" "rabbitmq_server_cert" {
  algorithm   = "ECDSA"
  ecdsa_curve = "P256"
}

resource "tls_self_signed_cert" "rabbitmq_debug_ca_cert" {
  key_algorithm     = tls_private_key.rabbitmq_debug_ca_key.algorithm
  private_key_pem   = tls_private_key.rabbitmq_debug_ca_key.private_key_pem
  is_ca_certificate = true

  subject {
    common_name  = "Motor.NET Debug CA"
    organization = "Motor.NET team"
  }

  validity_period_hours = 87659

  allowed_uses = [
    "digital_signature",
    "cert_signing",
    "crl_signing",
  ]
}

resource "tls_cert_request" rabbitmq_server_cert {
  key_algorithm   = tls_private_key.rabbitmq_server_cert.algorithm
  private_key_pem = tls_private_key.rabbitmq_server_cert.private_key_pem

  dns_names = [
    "localhost",
  ]

  subject {
    common_name         = "rabbitmq-tls-debug"
    organization        = "GDATA"
    country             = "DE"
    organizational_unit = "Platform Team"
  }
}

resource "tls_locally_signed_cert" "rabbitmq_server_cert" {
  cert_request_pem   = tls_cert_request.rabbitmq_server_cert.cert_request_pem
  ca_key_algorithm   = tls_private_key.rabbitmq_debug_ca_key.algorithm
  ca_private_key_pem = tls_private_key.rabbitmq_debug_ca_key.private_key_pem
  ca_cert_pem        = tls_self_signed_cert.rabbitmq_debug_ca_cert.cert_pem

  # Certificate expires after 12 hours.
  validity_period_hours = 12

  # Generate a new certificate if Terraform is run within three
  # hours of the certificate's expiration time.
  early_renewal_hours = 3

  allowed_uses = [
    "digital_signature",
    "key_encipherment",
    "server_auth",
    "client_auth",
  ]
}

resource "local_file" "rabbitmq_debug_ca_cert" {
  content  = tls_self_signed_cert.rabbitmq_debug_ca_cert.cert_pem
  filename = "${path.module}/certs/rabbitmq_debug_ca_cert.pem"
}

resource "local_file" "rabbitmq_debug_ca_key" {
  content  = tls_private_key.rabbitmq_debug_ca_key.private_key_pem
  filename = "${path.module}/certs/rabbitmq_debug_ca_key.pem"
}

resource "local_file" "rabbitmq_server_cert" {
  filename = "${path.module}/certs/rabbitmq_server_cert.pem"
  content  = tls_locally_signed_cert.rabbitmq_server_cert.cert_pem
}

resource "local_file" "rabbitmq_server_key" {
  filename = "${path.module}/certs/rabbitmq_server_key.pem"
  content  = tls_private_key.rabbitmq_server_cert.private_key_pem
}
