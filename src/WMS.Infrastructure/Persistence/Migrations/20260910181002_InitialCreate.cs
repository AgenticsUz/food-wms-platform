using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wms");

            migrationBuilder.CreateTable(
                name: "feature",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    module_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    default_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_custom = table.Column<bool>(type: "boolean", nullable: false),
                    owner_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    feature_codes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    trial_days = table.Column<int>(type: "integer", nullable: false),
                    max_users = table.Column<int>(type: "integer", nullable: false),
                    max_warehouses = table.Column<int>(type: "integer", nullable: false),
                    max_transfers_per_month = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    identity_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    modules = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    subscription_status = table.Column<int>(type: "integer", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trial_ends_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    paid_until = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    suspend_reason = table.Column<int>(type: "integer", nullable: true),
                    suspend_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    suspend_public_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    suspended_until = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    suspended_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    suspended_by_sub = table.Column<Guid>(type: "uuid", nullable: true),
                    logo_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    logo_square_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    brand_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "wms",
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_sub = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    is_platform_action = table.Column<bool>(type: "boolean", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "category",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_category_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "wms",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_category_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "driver",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    license_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver", x => x.id);
                    table.ForeignKey(
                        name: "fk_driver_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_record",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_by_sub = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_record_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_stage",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_stage", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_stage_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_parameter",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    min_value = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    max_value = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qc_parameter", x => x.id);
                    table.ForeignKey(
                        name: "fk_qc_parameter_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_feature",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    set_by_sub = table.Column<Guid>(type: "uuid", nullable: true),
                    set_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_feature", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_feature_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unit",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    short_name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_profile",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_sub = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    telegram_chat_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profile", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profile_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    capacity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse", x => x.id);
                    table.ForeignKey(
                        name: "fk_warehouse_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "wms",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permission_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    min_stock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    shelf_life_days = table.Column<int>(type: "integer", nullable: true),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    cost_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_category_category_id",
                        column: x => x.category_id,
                        principalSchema: "wms",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_unit_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "wms",
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    commission_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agent_user_profile_user_id",
                        column: x => x.user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "attendance_log",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    check_out = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    method = table.Column<int>(type: "integer", nullable: false),
                    device_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_log_shift_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "wms",
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_log_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_log_user_profile_user_id",
                        column: x => x.user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_user_profile_user_id",
                        column: x => x.user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_role_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "wms",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_role_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_role_user_profile_user_id",
                        column: x => x.user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    scheduled_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_driver_driver_id",
                        column: x => x.driver_id,
                        principalSchema: "wms",
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_user_profile_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "wms",
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "location",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location", x => x.id);
                    table.ForeignKey(
                        name: "fk_location_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_location_warehouse_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "wms",
                        principalTable: "warehouse",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "batch",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    manufactured_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    initial_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    remaining_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batch", x => x.id);
                    table.ForeignKey(
                        name: "fk_batch_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batch_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_recipe",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    output_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    output_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    output_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_recipe", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_recipe_product_output_product_id",
                        column: x => x.output_product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_recipe_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_recipe_unit_output_unit_id",
                        column: x => x.output_unit_id,
                        principalSchema: "wms",
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_actual",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    waste_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_actual", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_actual_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_actual_shift_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "wms",
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_actual_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_plan",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_plan", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_plan_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_plan_shift_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "wms",
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_plan_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "counterparty",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_counterparty", x => x.id);
                    table.ForeignKey(
                        name: "fk_counterparty_agent_agent_id",
                        column: x => x.agent_id,
                        principalSchema: "wms",
                        principalTable: "agent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_counterparty_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_stock",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_stock", x => x.id);
                    table.ForeignKey(
                        name: "fk_warehouse_stock_batch_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "wms",
                        principalTable: "batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warehouse_stock_location_location_id",
                        column: x => x.location_id,
                        principalSchema: "wms",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warehouse_stock_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warehouse_stock_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warehouse_stock_warehouse_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "wms",
                        principalTable: "warehouse",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_order",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    planned_start_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    planned_end_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_order", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_order_production_recipe_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "wms",
                        principalTable: "production_recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_order_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_order_user_profile_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_stage",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    output_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_output_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    allow_warehouse_output = table.Column<bool>(type: "boolean", nullable: false),
                    output_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_stage", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_stage_product_output_product_id",
                        column: x => x.output_product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_stage_production_recipe_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "wms",
                        principalTable: "production_recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_stage_production_stage_stage_id",
                        column: x => x.stage_id,
                        principalSchema: "wms",
                        principalTable: "production_stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_stage_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_stage_warehouse_output_warehouse_id",
                        column: x => x.output_warehouse_id,
                        principalSchema: "wms",
                        principalTable: "warehouse",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "debt",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_debt", x => x.id);
                    table.ForeignKey(
                        name: "fk_debt_counterparty_counterparty_id",
                        column: x => x.counterparty_id,
                        principalSchema: "wms",
                        principalTable: "counterparty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_debt_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    from_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    commission_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    return_reason = table.Column<int>(type: "integer", nullable: true),
                    original_transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfer", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfer_agent_agent_id",
                        column: x => x.agent_id,
                        principalSchema: "wms",
                        principalTable: "agent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_counterparty_counterparty_id",
                        column: x => x.counterparty_id,
                        principalSchema: "wms",
                        principalTable: "counterparty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_user_profile_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_warehouse_from_warehouse_id",
                        column: x => x.from_warehouse_id,
                        principalSchema: "wms",
                        principalTable: "warehouse",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_warehouse_to_warehouse_id",
                        column: x => x.to_warehouse_id,
                        principalSchema: "wms",
                        principalTable: "warehouse",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_stage_item",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_stage_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_stage_item_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_stage_item_recipe_stage_recipe_stage_id",
                        column: x => x.recipe_stage_id,
                        principalSchema: "wms",
                        principalTable: "recipe_stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_stage_item_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_stage_item_unit_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "wms",
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stage_execution",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    waste_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rework_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    worker_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stage_execution", x => x.id);
                    table.ForeignKey(
                        name: "fk_stage_execution_production_order_production_order_id",
                        column: x => x.production_order_id,
                        principalSchema: "wms",
                        principalTable: "production_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stage_execution_recipe_stage_recipe_stage_id",
                        column: x => x.recipe_stage_id,
                        principalSchema: "wms",
                        principalTable: "recipe_stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stage_execution_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stage_execution_user_profile_worker_user_id",
                        column: x => x.worker_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission_record",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_record_agent_agent_id",
                        column: x => x.agent_id,
                        principalSchema: "wms",
                        principalTable: "agent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commission_record_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commission_record_transfer_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "wms",
                        principalTable: "transfer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_stop",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sequence_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_stop", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_stop_counterparty_counterparty_id",
                        column: x => x.counterparty_id,
                        principalSchema: "wms",
                        principalTable: "counterparty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_stop_delivery_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "wms",
                        principalTable: "delivery",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_delivery_stop_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_stop_transfer_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "wms",
                        principalTable: "transfer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_history",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_history_counterparty_counterparty_id",
                        column: x => x.counterparty_id,
                        principalSchema: "wms",
                        principalTable: "counterparty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_history_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_history_transfer_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "wms",
                        principalTable: "transfer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_history_user_profile_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transaction",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    counterparty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transaction", x => x.id);
                    table.ForeignKey(
                        name: "fk_transaction_counterparty_counterparty_id",
                        column: x => x.counterparty_id,
                        principalSchema: "wms",
                        principalTable: "counterparty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transaction_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transaction_transfer_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "wms",
                        principalTable: "transfer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transaction_user_profile_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_item",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfer_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfer_item_batch_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "wms",
                        principalTable: "batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_item_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "wms",
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_item_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_item_transfer_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "wms",
                        principalTable: "transfer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qc_check",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_execution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parameter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_passed = table.Column<bool>(type: "boolean", nullable: false),
                    checked_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qc_check", x => x.id);
                    table.ForeignKey(
                        name: "fk_qc_check_qc_parameter_parameter_id",
                        column: x => x.parameter_id,
                        principalSchema: "wms",
                        principalTable: "qc_parameter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_qc_check_stage_execution_stage_execution_id",
                        column: x => x.stage_execution_id,
                        principalSchema: "wms",
                        principalTable: "stage_execution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_qc_check_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_qc_check_transfer_transfer_id",
                        column: x => x.transfer_id,
                        principalSchema: "wms",
                        principalTable: "transfer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_qc_check_user_profile_checked_by_user_id",
                        column: x => x.checked_by_user_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_tenant_id",
                schema: "wms",
                table: "agent",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_user_id",
                schema: "wms",
                table: "agent",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_log_shift_id",
                schema: "wms",
                table: "attendance_log",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_log_tenant_id",
                schema: "wms",
                table: "attendance_log",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_log_user_id",
                schema: "wms",
                table: "attendance_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_tenant_id_created_at",
                schema: "wms",
                table: "audit_log",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_batch_product_id",
                schema: "wms",
                table: "batch",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_batch_tenant_id_expiry_date",
                schema: "wms",
                table: "batch",
                columns: new[] { "tenant_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_batch_tenant_id_product_id",
                schema: "wms",
                table: "batch",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_category_parent_id",
                schema: "wms",
                table: "category",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_tenant_id",
                schema: "wms",
                table: "category",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_record_agent_id",
                schema: "wms",
                table: "commission_record",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_record_tenant_id_agent_id",
                schema: "wms",
                table: "commission_record",
                columns: new[] { "tenant_id", "agent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_record_tenant_id_transfer_id_agent_id",
                schema: "wms",
                table: "commission_record",
                columns: new[] { "tenant_id", "transfer_id", "agent_id" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_commission_record_transfer_id",
                schema: "wms",
                table: "commission_record",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_agent_id",
                schema: "wms",
                table: "counterparty",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_tenant_id",
                schema: "wms",
                table: "counterparty",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_debt_counterparty_id",
                schema: "wms",
                table: "debt",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_debt_tenant_id_counterparty_id",
                schema: "wms",
                table: "debt",
                columns: new[] { "tenant_id", "counterparty_id" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_created_by_user_id",
                schema: "wms",
                table: "delivery",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_driver_id",
                schema: "wms",
                table: "delivery",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_tenant_id_status",
                schema: "wms",
                table: "delivery",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_vehicle_id",
                schema: "wms",
                table: "delivery",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_stop_counterparty_id",
                schema: "wms",
                table: "delivery_stop",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_stop_delivery_id",
                schema: "wms",
                table: "delivery_stop",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_stop_tenant_id",
                schema: "wms",
                table: "delivery_stop",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_stop_transfer_id",
                schema: "wms",
                table: "delivery_stop",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_driver_tenant_id",
                schema: "wms",
                table: "driver",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_feature_code",
                schema: "wms",
                table: "feature",
                column: "code",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_location_tenant_id",
                schema: "wms",
                table: "location",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_location_warehouse_id",
                schema: "wms",
                table: "location",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_id_user_id_is_read",
                schema: "wms",
                table: "notification",
                columns: new[] { "tenant_id", "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_user_id",
                schema: "wms",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_counterparty_id",
                schema: "wms",
                table: "payment_history",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_recorded_by_user_id",
                schema: "wms",
                table: "payment_history",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_tenant_id_paid_at",
                schema: "wms",
                table: "payment_history",
                columns: new[] { "tenant_id", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_transfer_id",
                schema: "wms",
                table: "payment_history",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_record_tenant_id_period_end",
                schema: "wms",
                table: "payment_record",
                columns: new[] { "tenant_id", "period_end" });

            migrationBuilder.CreateIndex(
                name: "ix_plan_code",
                schema: "wms",
                table: "plan",
                column: "code",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_category_id",
                schema: "wms",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_tenant_id_barcode",
                schema: "wms",
                table: "product",
                columns: new[] { "tenant_id", "barcode" });

            migrationBuilder.CreateIndex(
                name: "ix_product_unit_id",
                schema: "wms",
                table: "product",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_order_assigned_to_user_id",
                schema: "wms",
                table: "production_order",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_order_recipe_id",
                schema: "wms",
                table: "production_order",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_order_tenant_id_status",
                schema: "wms",
                table: "production_order",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_production_recipe_output_product_id",
                schema: "wms",
                table: "production_recipe",
                column: "output_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_recipe_output_unit_id",
                schema: "wms",
                table: "production_recipe",
                column: "output_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_recipe_tenant_id",
                schema: "wms",
                table: "production_recipe",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_stage_tenant_id",
                schema: "wms",
                table: "production_stage",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_qc_check_checked_by_user_id",
                schema: "wms",
                table: "qc_check",
                column: "checked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_qc_check_parameter_id",
                schema: "wms",
                table: "qc_check",
                column: "parameter_id");

            migrationBuilder.CreateIndex(
                name: "ix_qc_check_stage_execution_id",
                schema: "wms",
                table: "qc_check",
                column: "stage_execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_qc_check_tenant_id",
                schema: "wms",
                table: "qc_check",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_qc_check_transfer_id",
                schema: "wms",
                table: "qc_check",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_qc_parameter_tenant_id",
                schema: "wms",
                table: "qc_parameter",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_output_product_id",
                schema: "wms",
                table: "recipe_stage",
                column: "output_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_output_warehouse_id",
                schema: "wms",
                table: "recipe_stage",
                column: "output_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_recipe_id",
                schema: "wms",
                table: "recipe_stage",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_stage_id",
                schema: "wms",
                table: "recipe_stage",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_tenant_id",
                schema: "wms",
                table: "recipe_stage",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_item_product_id",
                schema: "wms",
                table: "recipe_stage_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_item_recipe_stage_id",
                schema: "wms",
                table: "recipe_stage_item",
                column: "recipe_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_item_tenant_id",
                schema: "wms",
                table: "recipe_stage_item",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_stage_item_unit_id",
                schema: "wms",
                table: "recipe_stage_item",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_tenant_id_code",
                schema: "wms",
                table: "role",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "\"code\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_role_id_permission_code",
                schema: "wms",
                table: "role_permission",
                columns: new[] { "role_id", "permission_code" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_role_permission_tenant_id",
                schema: "wms",
                table: "role_permission",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_tenant_id",
                schema: "wms",
                table: "shift",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_actual_product_id",
                schema: "wms",
                table: "shift_actual",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_actual_shift_id",
                schema: "wms",
                table: "shift_actual",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_actual_tenant_id_date",
                schema: "wms",
                table: "shift_actual",
                columns: new[] { "tenant_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_plan_product_id",
                schema: "wms",
                table: "shift_plan",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_plan_shift_id",
                schema: "wms",
                table: "shift_plan",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_plan_tenant_id_date",
                schema: "wms",
                table: "shift_plan",
                columns: new[] { "tenant_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_stage_execution_production_order_id",
                schema: "wms",
                table: "stage_execution",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_stage_execution_recipe_stage_id",
                schema: "wms",
                table: "stage_execution",
                column: "recipe_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_stage_execution_tenant_id",
                schema: "wms",
                table: "stage_execution",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stage_execution_worker_user_id",
                schema: "wms",
                table: "stage_execution",
                column: "worker_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_code",
                schema: "wms",
                table: "tenant",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_id",
                schema: "wms",
                table: "tenant",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_feature_tenant_id_feature_code",
                schema: "wms",
                table: "tenant_feature",
                columns: new[] { "tenant_id", "feature_code" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_counterparty_id",
                schema: "wms",
                table: "transaction",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_recorded_by_user_id",
                schema: "wms",
                table: "transaction",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_tenant_id_date",
                schema: "wms",
                table: "transaction",
                columns: new[] { "tenant_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_transfer_id",
                schema: "wms",
                table: "transaction",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_agent_id",
                schema: "wms",
                table: "transfer",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_counterparty_id",
                schema: "wms",
                table: "transfer",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_created_by_user_id",
                schema: "wms",
                table: "transfer",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_from_warehouse_id",
                schema: "wms",
                table: "transfer",
                column: "from_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_tenant_id_confirmed_at",
                schema: "wms",
                table: "transfer",
                columns: new[] { "tenant_id", "confirmed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transfer_tenant_id_original_transfer_id",
                schema: "wms",
                table: "transfer",
                columns: new[] { "tenant_id", "original_transfer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_transfer_tenant_id_status",
                schema: "wms",
                table: "transfer",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_transfer_to_warehouse_id",
                schema: "wms",
                table: "transfer",
                column: "to_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_item_batch_id",
                schema: "wms",
                table: "transfer_item",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_item_product_id",
                schema: "wms",
                table: "transfer_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_item_tenant_id",
                schema: "wms",
                table: "transfer_item",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_item_transfer_id",
                schema: "wms",
                table: "transfer_item",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_tenant_id",
                schema: "wms",
                table: "unit",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_tenant_id_identity_sub",
                schema: "wms",
                table: "user_profile",
                columns: new[] { "tenant_id", "identity_sub" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                schema: "wms",
                table: "user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_tenant_id",
                schema: "wms",
                table: "user_role",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_user_id_role_id",
                schema: "wms",
                table: "user_role",
                columns: new[] { "user_id", "role_id" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_tenant_id",
                schema: "wms",
                table: "vehicle",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_tenant_id",
                schema: "wms",
                table: "warehouse",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_batch_id",
                schema: "wms",
                table: "warehouse_stock",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_location_id",
                schema: "wms",
                table: "warehouse_stock",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_product_id",
                schema: "wms",
                table: "warehouse_stock",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_tenant_id_warehouse_id_product_id",
                schema: "wms",
                table: "warehouse_stock",
                columns: new[] { "tenant_id", "warehouse_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_warehouse_id",
                schema: "wms",
                table: "warehouse_stock",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_log",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "commission_record",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "debt",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "delivery_stop",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "feature",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "notification",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "payment_history",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "payment_record",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "qc_check",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "recipe_stage_item",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "role_permission",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "shift_actual",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "shift_plan",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "tenant_feature",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "transaction",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "transfer_item",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "user_role",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "warehouse_stock",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "delivery",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "qc_parameter",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "stage_execution",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "shift",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "transfer",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "role",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "batch",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "location",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "driver",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "vehicle",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "production_order",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "recipe_stage",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "counterparty",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "production_recipe",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "production_stage",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "warehouse",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "agent",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "product",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "user_profile",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "category",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "unit",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "tenant",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "plan",
                schema: "wms");
        }
    }
}
