using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scriptoryum.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAIProviderConfigRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "a_i_configurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    open_a_i_api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    open_a_i_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    claude_api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    claude_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gemini_api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gemini_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    max_tokens = table.Column<string>(type: "text", nullable: true, defaultValue: "4000"),
                    temperature = table.Column<string>(type: "text", precision: 3, scale: 2, nullable: true, defaultValue: "0.7"),
                    default_provider = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_a_i_configurations", x => x.id);
                    table.ForeignKey(
                        name: "f_k_a_i_configurations_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    document_id = table.Column<int>(type: "integer", nullable: true),
                    message_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_chat_sessions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_chat_sessions_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_chat_sessions_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "a_i_provider_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    a_i_configuration_id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    selected_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_test_result = table.Column<bool>(type: "boolean", nullable: true),
                    last_test_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_tested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_a_i_provider_configs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_a_i_provider_configs_a_i_configurations_a_i_configuration_id",
                        column: x => x.a_i_configuration_id,
                        principalTable: "a_i_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chat_session_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    document_id = table.Column<int>(type: "integer", nullable: true),
                    document_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    token_count = table.Column<int>(type: "integer", nullable: true),
                    cost = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    a_i_provider = table.Column<string>(type: "text", nullable: true),
                    model_used = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_chat_messages_chat_sessions_chat_session_id",
                        column: x => x.chat_session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_chat_messages_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_a_i_configurations_user_id",
                table: "a_i_configurations",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_a_i_provider_configs_a_i_configuration_id",
                table: "a_i_provider_configs",
                column: "a_i_configuration_id");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_messages_chat_session_id",
                table: "chat_messages",
                column: "chat_session_id");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_messages_document_id",
                table: "chat_messages",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_document_id",
                table: "chat_sessions",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_user_id",
                table: "chat_sessions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "a_i_provider_configs");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "a_i_configurations");

            migrationBuilder.DropTable(
                name: "chat_sessions");
        }
    }
}
