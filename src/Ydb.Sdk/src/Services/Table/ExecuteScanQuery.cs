﻿using System.Collections.Generic;
using System.Linq;
using Ydb.Sdk.Client;
using Ydb.Sdk.Value;

namespace Ydb.Sdk.Table
{
    public class ExecuteScanQuerySettings : RequestSettings
    {
    }

    public class ExecuteScanQueryPart : ResponseWithResultBase<ExecuteScanQueryPart.ResultData>
    {
        internal ExecuteScanQueryPart(Status status, ResultData? result = null)
            : base(status, result)
        {
        }

        public class ResultData
        {
            internal ResultData(Value.ResultSet? resultSetPart)
            {
                ResultSetPart = resultSetPart;
            }

            public Value.ResultSet? ResultSetPart { get; }

            internal static ResultData FromProto(Ydb.Table.ExecuteScanQueryPartialResult resultProto)
            {
                return new ResultData(
                    resultProto.ResultSet is null ? null : Value.ResultSet.FromProto(resultProto.ResultSet));
            }
        }
    }

    public class ExecuteScanQueryStream : StreamResponse<Ydb.Table.ExecuteScanQueryPartialResponse, ExecuteScanQueryPart>
    {
        internal ExecuteScanQueryStream(Driver.StreamIterator<Ydb.Table.ExecuteScanQueryPartialResponse> iterator)
            : base(iterator)
        {
        }

        protected override ExecuteScanQueryPart MakeResponse(Status status)
        {
            return new ExecuteScanQueryPart(status);
        }

        protected override ExecuteScanQueryPart MakeResponse(Ydb.Table.ExecuteScanQueryPartialResponse protoResponse)
        {
            var status = Status.FromProto(protoResponse.Status, protoResponse.Issues);
            var result = status.IsSuccess && protoResponse.Result != null
                ? ExecuteScanQueryPart.ResultData.FromProto(protoResponse.Result)
                : null;

            return new ExecuteScanQueryPart(status, result);
        }
    }

    public partial class TableClient
    {
        public ExecuteScanQueryStream ExecuteScanQuery(
            string query,
            IReadOnlyDictionary<string, YdbValue> parameters,
            ExecuteScanQuerySettings? settings = null)
        {
            settings ??= new ExecuteScanQuerySettings();

            var request = new Ydb.Table.ExecuteScanQueryRequest
            {
                Mode = Ydb.Table.ExecuteScanQueryRequest.Types.Mode.Exec,
                Query = new Ydb.Table.Query
                {
                    YqlText = query
                }
            };

            request.Parameters.Add(parameters.ToDictionary(p => p.Key, p => p.Value.GetProto()));

            var streamIterator = Driver.StreamCall(
                method: Ydb.Table.V1.TableService.StreamExecuteScanQueryMethod,
                request: request,
                settings: settings);

            return new ExecuteScanQueryStream(streamIterator);
        }

        public ExecuteScanQueryStream ExecuteScanQuery(
            string query,
            ExecuteScanQuerySettings? settings = null)
        {
            return ExecuteScanQuery(query, new Dictionary<string, YdbValue>(), settings);
        }
    }
}